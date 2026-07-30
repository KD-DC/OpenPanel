using System.IO;
using System.Text.Json;

namespace OpenPanel.Host.Services;

public interface ISettingsService
{
    string Appearance { get; }
    WeatherLocationSettings WeatherLocation { get; }
    IReadOnlySet<string> DisabledWidgets { get; }
    Task SetAppearanceAsync(string appearance, CancellationToken cancellationToken);
    Task SetWidgetVisibilityAsync(
        string widgetId,
        bool isVisible,
        CancellationToken cancellationToken);
}

public sealed record WeatherLocationSettings(
    string Name,
    double Latitude,
    double Longitude);

public sealed class SettingsService : ISettingsService
{
    public const string CurrentAppearance = "current";
    public const string MediaOledAppearance = "mediaOled";
    public static WeatherLocationSettings DefaultWeatherLocation { get; } =
        new("Washington, DC", 38.9072, -77.0369);

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    private readonly string settingsPath;

    public SettingsService(string? settingsPath = null)
    {
        this.settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenPanel",
            "settings.json");
        var settings = LoadSettings();
        Appearance = IsSupportedAppearance(settings?.Appearance)
            ? settings!.Appearance
            : MediaOledAppearance;
        WeatherLocation = IsValidWeatherLocation(settings?.WeatherLocation)
            ? settings!.WeatherLocation!
            : DefaultWeatherLocation;
        DisabledWidgets = new HashSet<string>(
            settings?.DisabledWidgets?.Where(WidgetCatalog.Contains) ?? [],
            StringComparer.Ordinal);
    }

    public string Appearance { get; private set; }
    public WeatherLocationSettings WeatherLocation { get; }
    public IReadOnlySet<string> DisabledWidgets { get; private set; }

    public async Task SetAppearanceAsync(
        string appearance,
        CancellationToken cancellationToken)
    {
        if (!IsSupportedAppearance(appearance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(appearance),
                appearance,
                "OpenPanel does not support the requested appearance.");
        }

        if (string.Equals(Appearance, appearance, StringComparison.Ordinal))
        {
            return;
        }

        await SaveAsync(
            appearance,
            DisabledWidgets,
            cancellationToken);
        Appearance = appearance;
    }

    public async Task SetWidgetVisibilityAsync(
        string widgetId,
        bool isVisible,
        CancellationToken cancellationToken)
    {
        if (!WidgetCatalog.Contains(widgetId))
        {
            throw new ArgumentOutOfRangeException(
                nameof(widgetId),
                widgetId,
                "OpenPanel does not contain the requested widget.");
        }

        var disabled = new HashSet<string>(
            DisabledWidgets,
            StringComparer.Ordinal);
        var changed = isVisible
            ? disabled.Remove(widgetId)
            : disabled.Add(widgetId);
        if (!changed)
        {
            return;
        }

        await SaveAsync(Appearance, disabled, cancellationToken);
        DisabledWidgets = disabled;
    }

    private async Task SaveAsync(
        string appearance,
        IReadOnlySet<string> disabledWidgets,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(settingsPath) ??
            throw new InvalidOperationException("The settings path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = settingsPath + ".tmp";
        var json = JsonSerializer.Serialize(
            new PersistedSettings(
                appearance,
                WeatherLocation,
                disabledWidgets.OrderBy(widgetId => widgetId).ToArray()),
            JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, settingsPath, true);
    }

    private PersistedSettings? LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<PersistedSettings>(
                json,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool IsSupportedAppearance(string? appearance)
    {
        return appearance is CurrentAppearance or MediaOledAppearance;
    }

    private static bool IsValidWeatherLocation(WeatherLocationSettings? location)
    {
        return location is not null &&
            !string.IsNullOrWhiteSpace(location.Name) &&
            location.Latitude is >= -90 and <= 90 &&
            location.Longitude is >= -180 and <= 180;
    }

    private sealed record PersistedSettings(
        string Appearance,
        WeatherLocationSettings? WeatherLocation,
        IReadOnlyList<string>? DisabledWidgets = null);
}
