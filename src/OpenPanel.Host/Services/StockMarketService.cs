using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class StockMarketService : IDisposable
{
    private const int TradingDaySampleCount = 78;
    private static readonly TimeSpan OpenRefreshInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ClosedRefreshInterval = TimeSpan.FromMinutes(30);
    private readonly HttpClient httpClient;
    private readonly SecureIntegrationStore<StockConfiguration> store =
        new("stocks.dat");
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly Dictionary<string, Queue<StockPriceSample>> priceHistory =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly string historyPath;
    private DateOnly historyDate = DateOnly.FromDateTime(DateTime.Now);
    private StockConfiguration? configuration;
    private StockMarketSummary cached = Unavailable("Configure stocks from the system tray");
    private DateTimeOffset nextRefreshAt;

    public StockMarketService(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
        historyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenPanel",
            "Cache",
            "stock-history.json");
        LoadHistory();
        configuration = store.Load();
        if (configuration is not null)
        {
            cached = Unavailable("Waiting for market data") with { IsConfigured = true };
        }
    }

    public bool IsConfigured => configuration is not null;
    public string ApiKey => configuration?.ApiKey ?? "";
    public IReadOnlyList<string> Symbols => configuration?.Symbols ?? ["NVDA", "AMD", "MSFT", "TSLA"];

    public async Task ConfigureAsync(
        string apiKey,
        IEnumerable<string> symbols,
        CancellationToken cancellationToken)
    {
        var normalized = symbols
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Where(symbol => symbol.Length is > 0 and <= 15)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("A Twelve Data API key is required.", nameof(apiKey));
        }
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Enter at least one stock symbol.", nameof(symbols));
        }

        configuration = new StockConfiguration(apiKey.Trim(), normalized);
        await store.SaveAsync(configuration, cancellationToken);
        nextRefreshAt = DateTimeOffset.MinValue;
        cached = Unavailable("Waiting for market data") with { IsConfigured = true };
    }

    public async Task<StockMarketSummary> GetSnapshotAsync(
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!isActive)
        {
            return cached;
        }
        if (configuration is null)
        {
            return Unavailable("Configure stocks from the system tray");
        }
        if (DateTimeOffset.UtcNow < nextRefreshAt)
        {
            return cached;
        }
        if (!await refreshGate.WaitAsync(0, cancellationToken))
        {
            return cached;
        }

        try
        {
            if (DateTimeOffset.UtcNow < nextRefreshAt)
            {
                return cached;
            }
            try
            {
                var symbols = string.Join(',', configuration.Symbols);
                var url = "https://api.twelvedata.com/quote?" +
                    $"symbol={Uri.EscapeDataString(symbols)}&apikey={Uri.EscapeDataString(configuration.ApiKey)}";
                var json = await httpClient.GetStringAsync(url, cancellationToken);
                var quotes = ParseQuotes(json, configuration.Symbols);
                foreach (var quote in quotes)
                {
                    AddHistory(quote.Symbol, quote.Price);
                }
                await SaveHistoryAsync(cancellationToken);
                var enriched = quotes.Select(quote => quote with
                {
                    History = priceHistory.TryGetValue(quote.Symbol, out var history)
                        ? history.Select(sample => sample.Price).ToArray()
                        : []
                }).ToArray();
                var marketOpen = enriched.Any(quote => quote.IsMarketOpen);
                cached = new StockMarketSummary(
                    true,
                    true,
                    false,
                    marketOpen ? "Market open" : "Market closed",
                    marketOpen,
                    enriched,
                    DateTimeOffset.Now);
                nextRefreshAt = DateTimeOffset.UtcNow.Add(
                    marketOpen ? OpenRefreshInterval : ClosedRefreshInterval);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AppLog.Write("stocks.refresh.failed", ex.Message);
                cached = cached with
                {
                    IsConfigured = true,
                    IsAvailable = cached.Quotes.Count > 0,
                    IsStale = cached.Quotes.Count > 0,
                    Status = "Market update failed"
                };
                nextRefreshAt = DateTimeOffset.UtcNow.AddMinutes(1);
            }
            return cached;
        }
        finally
        {
            refreshGate.Release();
        }
    }

    internal static IReadOnlyList<StockQuoteSummary> ParseQuotes(
        string json,
        IReadOnlyList<string> symbols)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.TryGetProperty("status", out var status) &&
            string.Equals(status.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var message = root.TryGetProperty("message", out var errorMessage)
                ? errorMessage.GetString()
                : "Twelve Data returned an error.";
            throw new InvalidOperationException(message);
        }

        var quotes = new List<StockQuoteSummary>();
        foreach (var symbol in symbols)
        {
            var quote = root;
            if (symbols.Count > 1 && !root.TryGetProperty(symbol, out quote))
            {
                continue;
            }
            var price = Number(quote, "close");
            if (price is null)
            {
                continue;
            }
            quotes.Add(new StockQuoteSummary(
                symbol,
                Text(quote, "name") ?? symbol,
                price.Value,
                Number(quote, "change"),
                Number(quote, "percent_change"),
                Number(quote, "open"),
                Number(quote, "high"),
                Number(quote, "low"),
                Number(quote, "previous_close"),
                Boolean(quote, "is_market_open"),
                []));
        }
        return quotes;
    }

    public void Dispose()
    {
        refreshGate.Dispose();
        httpClient.Dispose();
    }

    private void AddHistory(string symbol, double price)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (historyDate != today)
        {
            priceHistory.Clear();
            historyDate = today;
        }
        if (!priceHistory.TryGetValue(symbol, out var history))
        {
            history = new Queue<StockPriceSample>();
            priceHistory[symbol] = history;
        }
        history.Enqueue(new StockPriceSample(DateTimeOffset.Now, price));
        while (history.Count > TradingDaySampleCount)
        {
            history.Dequeue();
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (!File.Exists(historyPath))
            {
                return;
            }
            var cache = JsonSerializer.Deserialize<StockHistoryCache>(
                File.ReadAllText(historyPath));
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (cache is null || cache.Date != today)
            {
                return;
            }

            historyDate = cache.Date;
            foreach (var (symbol, samples) in cache.Symbols)
            {
                priceHistory[symbol] = new Queue<StockPriceSample>(
                    samples.TakeLast(TradingDaySampleCount));
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            AppLog.Write("stocks.history.read.failed", ex.Message);
        }
    }

    private async Task SaveHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(historyPath) ??
                throw new InvalidOperationException("The stock history path has no directory.");
            Directory.CreateDirectory(directory);
            var cache = new StockHistoryCache(
                historyDate,
                priceHistory.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<StockPriceSample>)pair.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase));
            var temporaryPath = historyPath + ".tmp";
            await File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(cache),
                cancellationToken);
            File.Move(temporaryPath, historyPath, true);
        }
        catch (IOException ex)
        {
            AppLog.Write("stocks.history.write.failed", ex.Message);
        }
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) ? property.GetString() : null;

    private static double? Number(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }
        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var number))
        {
            return number;
        }
        return double.TryParse(
            property.GetString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out number)
            ? number
            : null;
    }

    private static bool Boolean(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return false;
        }
        return property.ValueKind == JsonValueKind.True ||
            (property.ValueKind == JsonValueKind.String &&
             bool.TryParse(property.GetString(), out var result) && result);
    }

    private static StockMarketSummary Unavailable(string status) =>
        new(false, false, false, status, false, [], null);

    private sealed record StockConfiguration(
        string ApiKey,
        IReadOnlyList<string> Symbols);

    private sealed record StockPriceSample(
        DateTimeOffset Time,
        double Price);

    private sealed record StockHistoryCache(
        DateOnly Date,
        IReadOnlyDictionary<string, IReadOnlyList<StockPriceSample>> Symbols);
}
