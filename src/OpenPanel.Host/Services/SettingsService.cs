using System.IO;
using System.Text.Json;

namespace OpenPanel.Host.Services;

public interface ISettingsService
{
    string Appearance { get; }
    WeatherLocationSettings WeatherLocation { get; }
    Task SetAppearanceAsync(string appearance, CancellationToken cancellationToken);
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
    }

    public string Appearance { get; private set; }
    public WeatherLocationSettings WeatherLocation { get; }

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

        var directory = Path.GetDirectoryName(settingsPath) ??
            throw new InvalidOperationException("The settings path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = settingsPath + ".tmp";
        var json = JsonSerializer.Serialize(
            new PersistedSettings(appearance, WeatherLocation),
            JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, settingsPath, true);
        Appearance = appearance;
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
        WeatherLocationSettings? WeatherLocation);
}
