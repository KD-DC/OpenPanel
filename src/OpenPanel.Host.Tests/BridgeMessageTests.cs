using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Messaging;
using OpenPanel.Host.Models;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class BridgeMessageTests
{
    [TestMethod]
    public void StateUpdateSerializesWithCamelCaseFields()
    {
        var telemetry = new HardwareTelemetrySnapshot(
            new TelemetrySummary(18, 43, 21.4, 64, 4.2, 82.5),
            new GpuSummary(36, 51, 6.8, 16, 172, 1220),
            new AdvancedTelemetrySummary(
                new MemorySummary(21.4, 42.6, 64, 33.4, 27, 72),
                4200,
                88,
                2505,
                10501,
                42,
                67,
                72),
            new StorageSummary(
                [new StorageDeviceSummary("System NVMe", 62, 14, 41, 128.5, 32.25)]));
        var display = new DisplaySummary("ASUS target", 0, 0, 1920, 550, false);
        var media = new MediaSummary(
            "Spotify",
            "Test Track",
            "Test Artist",
            "Test Album",
            "Test Album Artist",
            "Test Subtitle",
            ["Electronic"],
            4,
            12,
            "Playing",
            "Music",
            true,
            "List",
            1,
            null,
            true,
            30,
            180,
            true,
            true,
            true,
            true,
            true);
        var audio = new AudioSummary(
            "device-1",
            "Desk Speakers",
            42,
            false,
            12,
            true,
            [new AudioOutputSummary("device-1", "Desk Speakers", true)],
            "input-1",
            "Desk Microphone",
            68,
            false,
            9,
            [new AudioInputSummary("input-1", "Desk Microphone", true)],
            [new AudioSessionSummary("process:42", "Spotify", 55, false, 18)]);
        var weather = new WeatherSummary(
            "Washington, DC",
            true,
            false,
            "Updated",
            81,
            84,
            58,
            7,
            1,
            [new HourlyForecastSummary(
                new DateTime(2026, 7, 29, 18, 0, 0),
                80,
                1,
                10)],
            [new DailyForecastSummary(
                new DateOnly(2026, 7, 29),
                88,
                72,
                1,
                20)],
            new AirQualitySummary(42, "Good", 8, 15, 61),
            new DateTimeOffset(2026, 7, 29, 17, 0, 0, TimeSpan.FromHours(-4)));
        var payload = new DashboardStateProvider().CreateState(
            telemetry,
            new NetworkQualitySummary(
                true,
                true,
                "Online",
                "Ethernet",
                "Ethernet",
                "192.168.1.20",
                2500,
                12.4,
                1.8,
                0,
                "1.1.1.1",
                new NetworkApplicationTrafficSummary(
                    true,
                    true,
                    false,
                    "Live app traffic",
                    [new NetworkApplicationSummary(
                        4120,
                        "Spotify",
                        0.1,
                        4.2)])),
            new ProcessUsageSummary(
                true,
                "Live application usage",
                [new ProcessUsageApplicationSummary("Browser", 8.5, 640)],
                [new ProcessUsageApplicationSummary("Editor", 2.1, 1024)]),
            new PeripheralBatterySummary(
                [new PeripheralBatteryDeviceSummary(
                    "logitech:c52b:1",
                    "MX Master",
                    "Mouse",
                    82,
                    false,
                    true,
                    "Logitech HID++")],
                DateTimeOffset.Now),
            new GamingPerformanceSummary(
                false,
                true,
                "Tap start to monitor a game",
                "",
                null,
                null,
                null,
                null,
                0,
                null),
            media,
            audio,
            weather,
            SettingsService.MediaOledAppearance,
            WidgetCatalog.CreateSummary(new HashSet<string> { "gaming" }),
            display);
        var message = new HostToUiMessage("state:update", payload);

        var json = JsonSerializer.Serialize(message, MessageJson.Options);

        StringAssert.Contains(json, "\"type\":\"state:update\"");
        StringAssert.Contains(json, "\"cpuUsagePercent\":18");
        StringAssert.Contains(json, "\"gpuUsagePercent\":36");
        StringAssert.Contains(json, "\"availableGb\":42.6");
        StringAssert.Contains(json, "\"cpuPackagePowerWatts\":88");
        StringAssert.Contains(json, "\"name\":\"System NVMe\"");
        StringAssert.Contains(json, "\"usedPercent\":62");
        StringAssert.Contains(json, "\"readMegabytesPerSecond\":128.5");
        StringAssert.Contains(json, "\"title\":\"Test Track\"");
        StringAssert.Contains(json, "\"albumArtist\":\"Test Album Artist\"");
        StringAssert.Contains(json, "\"genres\":[\"Electronic\"]");
        StringAssert.Contains(json, "\"trackNumber\":4");
        StringAssert.Contains(json, "\"playbackStatus\":\"Playing\"");
        StringAssert.Contains(json, "\"isShuffleActive\":true");
        StringAssert.Contains(json, "\"repeatMode\":\"List\"");
        StringAssert.Contains(json, "\"canShuffle\":true");
        StringAssert.Contains(json, "\"appearance\":{\"theme\":\"mediaOled\"}");
        StringAssert.Contains(json, "\"currentOutput\":\"Desk Speakers\"");
        StringAssert.Contains(json, "\"peakLevelPercent\":12");
        StringAssert.Contains(json, "\"precipitationProbabilityPercent\":20");
        StringAssert.Contains(json, "\"currentInput\":\"Desk Microphone\"");
        StringAssert.Contains(json, "\"inputPeakLevelPercent\":9");
        StringAssert.Contains(json, "\"name\":\"Spotify\"");
        StringAssert.Contains(json, "\"location\":\"Washington, DC\"");
        StringAssert.Contains(json, "\"currentTemperatureFahrenheit\":81");
        StringAssert.Contains(json, "\"usAqi\":42");
        StringAssert.Contains(json, "\"latencyMs\":12.4");
        StringAssert.Contains(json, "\"cpuPercent\":8.5");
        StringAssert.Contains(json, "\"memoryMegabytes\":1024");
        StringAssert.Contains(json, "\"batteryPercent\":82");
        StringAssert.Contains(json, "\"collectorAvailable\":true");
        StringAssert.Contains(
            json,
            "\"id\":\"gaming\",\"label\":\"Gaming performance\",\"isVisible\":false");
    }

    [TestMethod]
    public void MediaShuffleCommandDeserializesTypedPayload()
    {
        const string json =
            """{"type":"command:media.shuffle","payload":{"isActive":true}}""";

        var command = JsonSerializer.Deserialize<UiToHostMessage>(json, MessageJson.Options);
        var payload = command?.Payload?.Deserialize<MediaShufflePayload>(MessageJson.Options);

        Assert.IsNotNull(command);
        Assert.AreEqual("command:media.shuffle", command.Type);
        Assert.IsNotNull(payload);
        Assert.IsTrue(payload.IsActive);
    }

    [TestMethod]
    public void AudioSelectCommandDeserializesTypedPayload()
    {
        const string json =
            """{"type":"command:audio.select","payload":{"outputId":"device-2","setCommunicationsDevice":false}}""";

        var command = JsonSerializer.Deserialize<UiToHostMessage>(json, MessageJson.Options);
        var payload = command?.Payload?.Deserialize<AudioSelectPayload>(MessageJson.Options);

        Assert.IsNotNull(command);
        Assert.AreEqual("command:audio.select", command.Type);
        Assert.IsNotNull(payload);
        Assert.AreEqual("device-2", payload.OutputId);
        Assert.IsFalse(payload.SetCommunicationsDevice);
    }

    [TestMethod]
    public void NetworkExpandedCommandDeserializesTypedPayload()
    {
        const string json =
            """{"type":"command:network.expanded","payload":{"isExpanded":true}}""";

        var command = JsonSerializer.Deserialize<UiToHostMessage>(json, MessageJson.Options);
        var payload = command?.Payload?.Deserialize<NetworkExpandedPayload>(MessageJson.Options);

        Assert.IsNotNull(payload);
        Assert.IsTrue(payload.IsExpanded);
    }

    [TestMethod]
    public void HardwareExpandedCommandDeserializesTypedPayload()
    {
        const string json =
            """{"type":"command:hardware.expanded","payload":{"isExpanded":true}}""";

        var command = JsonSerializer.Deserialize<UiToHostMessage>(json, MessageJson.Options);
        var payload = command?.Payload?.Deserialize<HardwareExpandedPayload>(MessageJson.Options);

        Assert.IsNotNull(payload);
        Assert.IsTrue(payload.IsExpanded);
    }

    [TestMethod]
    public void GamingActiveCommandDeserializesTypedPayload()
    {
        const string json =
            """{"type":"command:gaming.active","payload":{"isActive":false}}""";

        var command = JsonSerializer.Deserialize<UiToHostMessage>(json, MessageJson.Options);
        var payload = command?.Payload?.Deserialize<GamingActivePayload>(MessageJson.Options);

        Assert.IsNotNull(payload);
        Assert.IsFalse(payload.IsActive);
    }

    [TestMethod]
    public void AudioSessionVolumeCommandDeserializesTypedPayload()
    {
        const string json =
            """{"type":"command:audio.session.volume","payload":{"sessionId":"process:42","volumePercent":35}}""";

        var command = JsonSerializer.Deserialize<UiToHostMessage>(json, MessageJson.Options);
        var payload = command?.Payload?.Deserialize<AudioSessionVolumePayload>(
            MessageJson.Options);

        Assert.IsNotNull(command);
        Assert.AreEqual("command:audio.session.volume", command.Type);
        Assert.IsNotNull(payload);
        Assert.AreEqual("process:42", payload.SessionId);
        Assert.AreEqual(35, payload.VolumePercent);
    }

    [TestMethod]
    public void AudioExpandedCommandDeserializesTypedPayload()
    {
        const string json =
            """{"type":"command:audio.expanded","payload":{"isExpanded":true}}""";

        var command = JsonSerializer.Deserialize<UiToHostMessage>(json, MessageJson.Options);
        var payload = command?.Payload?.Deserialize<AudioExpandedPayload>(MessageJson.Options);

        Assert.IsNotNull(command);
        Assert.AreEqual("command:audio.expanded", command.Type);
        Assert.IsNotNull(payload);
        Assert.IsTrue(payload.IsExpanded);
    }
}
