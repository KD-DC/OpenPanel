using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class GoogleCalendarService : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private const string CalendarScope =
        "https://www.googleapis.com/auth/calendar.events.readonly " +
        "https://www.googleapis.com/auth/calendar.calendarlist.readonly " +
        "https://www.googleapis.com/auth/tasks.readonly";

    private readonly HttpClient httpClient;
    private readonly SecureIntegrationStore<GoogleCalendarConfiguration> store =
        new("google-calendar.dat");
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private GoogleCalendarConfiguration? configuration;
    private CalendarSummary cached = Unavailable("Connect Google Tasks / Calendar from the system tray");
    private string? accessToken;
    private DateTimeOffset accessTokenExpiresAt;
    private DateTimeOffset nextRefreshAt;

    public GoogleCalendarService(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
        configuration = store.Load();
        if (configuration?.RefreshToken is not null)
        {
            cached = Unavailable("Waiting for calendar sync") with { IsConnected = true };
        }
    }

    public bool IsConnected => !string.IsNullOrWhiteSpace(configuration?.RefreshToken);
    public bool HasTasksAccess => configuration?.IncludesTasks == true;

    public async Task ConnectAsync(
        string credentialsJson,
        CancellationToken cancellationToken)
    {
        var credentials = ParseCredentials(credentialsJson);
        var token = await AuthorizeAsync(credentials, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            throw new InvalidOperationException(
                "Google did not return a refresh token. Remove OpenPanel access from your Google account and connect again.");
        }

        configuration = new GoogleCalendarConfiguration(
            credentials.ClientId,
            credentials.ClientSecret,
            credentials.TokenUri,
            token.RefreshToken,
            ["primary"],
            true);
        accessToken = token.AccessToken;
        accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds - 60);
        nextRefreshAt = DateTimeOffset.MinValue;
        await store.SaveAsync(configuration, cancellationToken);
        cached = Unavailable("Waiting for calendar sync") with { IsConnected = true };
    }

    public async Task<IReadOnlyList<GoogleCalendarOption>> GetCalendarsAsync(
        CancellationToken cancellationToken)
    {
        await EnsureAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://www.googleapis.com/calendar/v3/users/me/calendarList?" +
            "minAccessRole=reader&showDeleted=false&showHidden=false&maxResults=250");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var selectedIds = new HashSet<string>(
            configuration?.SelectedCalendarIds ?? ["primary"],
            StringComparer.Ordinal);
        return items.EnumerateArray()
            .Select(item => new GoogleCalendarOption(
                RequiredString(item, "id"),
                item.TryGetProperty("summaryOverride", out var summaryOverride) &&
                    !string.IsNullOrWhiteSpace(summaryOverride.GetString())
                    ? summaryOverride.GetString()!
                    : RequiredString(item, "summary"),
                item.TryGetProperty("primary", out var primary) && primary.GetBoolean(),
                selectedIds.Contains(RequiredString(item, "id")) ||
                    (selectedIds.Contains("primary") &&
                     item.TryGetProperty("primary", out var isPrimary) &&
                     isPrimary.GetBoolean())))
            .OrderByDescending(calendar => calendar.IsPrimary)
            .ThenBy(calendar => calendar.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public async Task SetSelectedCalendarsAsync(
        IReadOnlyList<string> calendarIds,
        CancellationToken cancellationToken)
    {
        var config = configuration ??
            throw new InvalidOperationException("Google Tasks / Calendar is not connected.");
        var selected = calendarIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (selected.Length == 0)
        {
            throw new ArgumentException("Select at least one calendar.", nameof(calendarIds));
        }

        configuration = config with { SelectedCalendarIds = selected };
        await store.SaveAsync(configuration, cancellationToken);
        nextRefreshAt = DateTimeOffset.MinValue;
    }

    public void Disconnect()
    {
        configuration = null;
        accessToken = null;
        accessTokenExpiresAt = DateTimeOffset.MinValue;
        nextRefreshAt = DateTimeOffset.MinValue;
        cached = Unavailable("Connect Google Tasks / Calendar from the system tray");
        store.Delete();
    }

    public async Task<CalendarSummary> GetSnapshotAsync(
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!isActive)
        {
            return cached;
        }
        if (!IsConnected)
        {
            return Unavailable("Connect Google Tasks / Calendar from the system tray");
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
                await EnsureAccessTokenAsync(cancellationToken);
                cached = await FetchTodayAsync(cancellationToken);
                nextRefreshAt = DateTimeOffset.UtcNow.Add(RefreshInterval);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AppLog.Write("calendar.refresh.failed", ex.Message);
                cached = cached with
                {
                    IsConnected = true,
                    IsAvailable = cached.Events.Count > 0,
                    IsStale = cached.Events.Count > 0,
                    Status = "Calendar update failed"
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

    internal static GoogleOAuthCredentials ParseCredentials(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("installed", out var installed))
        {
            throw new InvalidOperationException(
                "Select OAuth credentials created as a Google Desktop app.");
        }

        var clientId = RequiredString(installed, "client_id");
        var clientSecret = RequiredString(installed, "client_secret");
        var tokenUri = RequiredString(installed, "token_uri");
        return new GoogleOAuthCredentials(clientId, clientSecret, tokenUri);
    }

    public void Dispose()
    {
        refreshGate.Dispose();
        httpClient.Dispose();
    }

    private async Task<OAuthToken> AuthorizeAsync(
        GoogleOAuthCredentials credentials,
        CancellationToken cancellationToken)
    {
        var port = ReserveLoopbackPort();
        var redirectUri = $"http://127.0.0.1:{port}/oauth2/callback/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        var state = Base64Url(RandomNumberGenerator.GetBytes(24));
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var authorizationUri =
            "https://accounts.google.com/o/oauth2/v2/auth?" +
            Form(new Dictionary<string, string>
            {
                ["client_id"] = credentials.ClientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = CalendarScope,
                ["access_type"] = "offline",
                ["prompt"] = "consent",
                ["state"] = state,
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256"
            });

        Process.Start(new ProcessStartInfo(authorizationUri) { UseShellExecute = true });
        var context = await listener.GetContextAsync()
            .WaitAsync(TimeSpan.FromMinutes(3), cancellationToken);
        var query = context.Request.QueryString;
        var responseHtml = "<html><body style='background:#05070a;color:#fff;font:20px Segoe UI;padding:40px'>Google Tasks / Calendar is connected. You can close this window.</body></html>";
        var responseBytes = Encoding.UTF8.GetBytes(responseHtml);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = responseBytes.Length;
        await context.Response.OutputStream.WriteAsync(responseBytes, cancellationToken);
        context.Response.Close();

        if (!string.Equals(query["state"], state, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Google OAuth returned an invalid state value.");
        }
        if (!string.IsNullOrWhiteSpace(query["error"]))
        {
            throw new InvalidOperationException($"Google authorization failed: {query["error"]}");
        }

        var code = query["code"] ??
            throw new InvalidOperationException("Google authorization did not return a code.");
        return await ExchangeTokenAsync(
            credentials,
            new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = credentials.ClientId,
                ["client_secret"] = credentials.ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = verifier
            },
            cancellationToken);
    }

    private async Task EnsureAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken) &&
            accessTokenExpiresAt > DateTimeOffset.UtcNow)
        {
            return;
        }

        var config = configuration ??
            throw new InvalidOperationException("Google Tasks / Calendar is not configured.");
        var token = await ExchangeTokenAsync(
            new GoogleOAuthCredentials(config.ClientId, config.ClientSecret, config.TokenUri),
            new Dictionary<string, string>
            {
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["refresh_token"] = config.RefreshToken,
                ["grant_type"] = "refresh_token"
            },
            cancellationToken);
        accessToken = token.AccessToken;
        accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds - 60);
    }

    private async Task<OAuthToken> ExchangeTokenAsync(
        GoogleOAuthCredentials credentials,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            credentials.TokenUri,
            new FormUrlEncodedContent(values),
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google token request failed ({(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new OAuthToken(
            RequiredString(root, "access_token"),
            root.TryGetProperty("refresh_token", out var refreshToken)
                ? refreshToken.GetString()
                : null,
            root.TryGetProperty("expires_in", out var expiresIn)
                ? expiresIn.GetInt32()
                : 3600);
    }

    private async Task<CalendarSummary> FetchTodayAsync(CancellationToken cancellationToken)
    {
        var start = DateTime.Today;
        var end = start.AddDays(1);
        var events = new List<CalendarEventSummary>();
        var calendarIds = configuration?.SelectedCalendarIds is { Count: > 0 } selected
            ? selected
            : ["primary"];
        foreach (var calendarId in calendarIds)
        {
            var url = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events?" +
                Form(new Dictionary<string, string>
                {
                    ["timeMin"] = start.ToUniversalTime().ToString("O"),
                    ["timeMax"] = end.ToUniversalTime().ToString("O"),
                    ["singleEvents"] = "true",
                    ["orderBy"] = "startTime",
                    ["maxResults"] = "30"
                });
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            events.AddRange(ParseEvents(json));
        }
        if (HasTasksAccess)
        {
            events.AddRange(await FetchTodayTasksAsync(DateOnly.FromDateTime(start), cancellationToken));
        }
        events = events
            .OrderBy(item => item.IsAllDay ? (item.IsTask ? 1 : 0) : 2)
            .ThenBy(item => item.Start)
            .ToList();
        return new CalendarSummary(
            true,
            true,
            false,
            events.Count == 0 ? "No events today" : "Updated",
            events,
            DateTimeOffset.Now);
    }

    internal static IReadOnlyList<CalendarEventSummary> ParseEvents(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var events = new List<CalendarEventSummary>();
        foreach (var item in items.EnumerateArray())
        {
            if (!TryReadEventTime(item, "start", out var start, out var allDay) ||
                !TryReadEventTime(item, "end", out var end, out _))
            {
                continue;
            }

            events.Add(new CalendarEventSummary(
                item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                item.TryGetProperty("summary", out var summary)
                    ? summary.GetString() ?? "Busy"
                    : "Busy",
                item.TryGetProperty("location", out var location)
                    ? location.GetString() ?? ""
                    : "",
                start,
                end,
                allDay,
                false,
                item.TryGetProperty("htmlLink", out var link)
                    ? link.GetString()
                    : null));
        }
        return events;
    }

    private async Task<IReadOnlyList<CalendarEventSummary>> FetchTodayTasksAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        using var listsRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "https://tasks.googleapis.com/tasks/v1/users/@me/lists?maxResults=100");
        listsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var listsResponse = await httpClient.SendAsync(listsRequest, cancellationToken);
        listsResponse.EnsureSuccessStatusCode();
        var listsJson = await listsResponse.Content.ReadAsStringAsync(cancellationToken);
        var taskLists = ParseTaskLists(listsJson);
        var tasks = new List<CalendarEventSummary>();
        foreach (var taskList in taskLists)
        {
            var url = $"https://tasks.googleapis.com/tasks/v1/lists/{Uri.EscapeDataString(taskList.Id)}/tasks?" +
                Form(new Dictionary<string, string>
                {
                    ["dueMin"] = $"{date:yyyy-MM-dd}T00:00:00Z",
                    ["dueMax"] = $"{date.AddDays(1):yyyy-MM-dd}T00:00:00Z",
                    ["showCompleted"] = "false",
                    ["showDeleted"] = "false",
                    ["showHidden"] = "false",
                    ["showAssigned"] = "true",
                    ["maxResults"] = "100"
                });
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            tasks.AddRange(ParseTasks(json, taskList, date));
        }
        return tasks;
    }

    internal static IReadOnlyList<GoogleTaskList> ParseTaskLists(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }
        return items.EnumerateArray()
            .Select(item => new GoogleTaskList(
                RequiredString(item, "id"),
                RequiredString(item, "title")))
            .ToArray();
    }

    internal static IReadOnlyList<CalendarEventSummary> ParseTasks(
        string json,
        GoogleTaskList taskList,
        DateOnly date)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var startDateTime = date.ToDateTime(TimeOnly.MinValue);
        var start = new DateTimeOffset(startDateTime, TimeZoneInfo.Local.GetUtcOffset(startDateTime));
        return items.EnumerateArray()
            .Where(item => !item.TryGetProperty("status", out var status) ||
                !string.Equals(status.GetString(), "completed", StringComparison.OrdinalIgnoreCase))
            .Where(item => item.TryGetProperty("due", out var due) &&
                DateTimeOffset.TryParse(due.GetString(), out var dueDate) &&
                DateOnly.FromDateTime(dueDate.UtcDateTime) == date)
            .Select(item => new CalendarEventSummary(
                $"task:{taskList.Id}:{RequiredString(item, "id")}",
                item.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString())
                    ? title.GetString()!
                    : "Untitled task",
                taskList.Title,
                start,
                start.AddDays(1),
                true,
                true,
                item.TryGetProperty("webViewLink", out var link) ? link.GetString() : null))
            .ToArray();
    }

    private static bool TryReadEventTime(
        JsonElement item,
        string propertyName,
        out DateTimeOffset value,
        out bool allDay)
    {
        value = default;
        allDay = false;
        if (!item.TryGetProperty(propertyName, out var time))
        {
            return false;
        }
        if (time.TryGetProperty("dateTime", out var dateTime) &&
            DateTimeOffset.TryParse(dateTime.GetString(), out value))
        {
            return true;
        }
        if (time.TryGetProperty("date", out var date) &&
            DateOnly.TryParse(date.GetString(), out var dateOnly))
        {
            allDay = true;
            value = new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local.GetUtcOffset(dateOnly.ToDateTime(TimeOnly.MinValue)));
            return true;
        }
        return false;
    }

    private static CalendarSummary Unavailable(string status) =>
        new(false, false, false, status, [], null);

    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException($"Google credentials are missing {propertyName}.");
        }
        return property.GetString()!;
    }

    private static string Form(IReadOnlyDictionary<string, string> values) =>
        string.Join("&", values.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    internal sealed record GoogleOAuthCredentials(
        string ClientId,
        string ClientSecret,
        string TokenUri);

    internal sealed record GoogleTaskList(string Id, string Title);

    private sealed record GoogleCalendarConfiguration(
        string ClientId,
        string ClientSecret,
        string TokenUri,
        string RefreshToken,
        IReadOnlyList<string>? SelectedCalendarIds = null,
        bool IncludesTasks = false);

    private sealed record OAuthToken(
        string AccessToken,
        string? RefreshToken,
        int ExpiresInSeconds);
}

public sealed record GoogleCalendarOption(
    string Id,
    string Name,
    bool IsPrimary,
    bool IsSelected);
