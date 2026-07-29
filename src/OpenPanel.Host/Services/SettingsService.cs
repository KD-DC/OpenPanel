using System.IO;
using System.Text.Json;

namespace OpenPanel.Host.Services;

public interface ISettingsService
{
    string Appearance { get; }
    Task SetAppearanceAsync(string appearance, CancellationToken cancellationToken);
}

public sealed class SettingsService : ISettingsService
{
    public const string CurrentAppearance = "current";
    public const string MediaOledAppearance = "mediaOled";

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
        Appearance = LoadAppearance();
    }

    public string Appearance { get; private set; }

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
            new PersistedSettings(appearance),
            JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, settingsPath, true);
        Appearance = appearance;
    }

    private string LoadAppearance()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return MediaOledAppearance;
            }

            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<PersistedSettings>(
                json,
                JsonOptions);
            var appearance = settings?.Appearance;
            return IsSupportedAppearance(appearance)
                ? appearance!
                : MediaOledAppearance;
        }
        catch (JsonException)
        {
            return MediaOledAppearance;
        }
        catch (IOException)
        {
            return MediaOledAppearance;
        }
    }

    private static bool IsSupportedAppearance(string? appearance)
    {
        return appearance is CurrentAppearance or MediaOledAppearance;
    }

    private sealed record PersistedSettings(string Appearance);
}
