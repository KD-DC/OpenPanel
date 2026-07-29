using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class SettingsServiceTests
{
    private string? temporaryDirectory;

    [TestCleanup]
    public void Cleanup()
    {
        if (temporaryDirectory is not null && Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, true);
        }
    }

    [TestMethod]
    public void MissingSettingsDefaultToMediaOled()
    {
        var service = CreateService();

        Assert.AreEqual(SettingsService.MediaOledAppearance, service.Appearance);
        Assert.AreEqual("Washington, DC", service.WeatherLocation.Name);
    }

    [TestMethod]
    public async Task AppearancePersistsAcrossInstances()
    {
        var settingsPath = CreateSettingsPath();
        var service = new SettingsService(settingsPath);

        await service.SetAppearanceAsync(
            SettingsService.CurrentAppearance,
            CancellationToken.None);

        var reloaded = new SettingsService(settingsPath);
        Assert.AreEqual(SettingsService.CurrentAppearance, reloaded.Appearance);
    }

    [TestMethod]
    public void InvalidPersistedAppearanceFallsBackToMediaOled()
    {
        var settingsPath = CreateSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(
            settingsPath,
            JsonSerializer.Serialize(new { appearance = "unsupported" }));

        var service = new SettingsService(settingsPath);

        Assert.AreEqual(SettingsService.MediaOledAppearance, service.Appearance);
    }

    [TestMethod]
    public async Task UnsupportedAppearanceIsRejected()
    {
        var service = CreateService();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.SetAppearanceAsync("unsupported", CancellationToken.None));
    }

    [TestMethod]
    public void ValidWeatherLocationLoadsFromSettings()
    {
        var settingsPath = CreateSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(
            settingsPath,
            """
            {
              "appearance": "mediaOled",
              "weatherLocation": {
                "name": "Baltimore, MD",
                "latitude": 39.2904,
                "longitude": -76.6122
              }
            }
            """);

        var service = new SettingsService(settingsPath);

        Assert.AreEqual("Baltimore, MD", service.WeatherLocation.Name);
        Assert.AreEqual(39.2904, service.WeatherLocation.Latitude);
        Assert.AreEqual(-76.6122, service.WeatherLocation.Longitude);
    }

    private SettingsService CreateService()
    {
        return new SettingsService(CreateSettingsPath());
    }

    private string CreateSettingsPath()
    {
        temporaryDirectory ??= Path.Combine(
            Path.GetTempPath(),
            "OpenPanel.Tests",
            Guid.NewGuid().ToString("N"));
        return Path.Combine(temporaryDirectory, "settings.json");
    }
}
