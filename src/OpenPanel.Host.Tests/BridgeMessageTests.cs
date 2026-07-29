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
            [new AudioOutputSummary("device-1", "Desk Speakers", true)]);
        var payload = new DashboardStateProvider().CreateState(
            telemetry,
            media,
            audio,
            SettingsService.MediaOledAppearance,
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
}
