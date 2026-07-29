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
                72));
        var display = new DisplaySummary("ASUS target", 0, 0, 1920, 550, false);
        var payload = new DashboardStateProvider().CreateState(telemetry, display);
        var message = new HostToUiMessage("state:update", payload);

        var json = JsonSerializer.Serialize(message, MessageJson.Options);

        StringAssert.Contains(json, "\"type\":\"state:update\"");
        StringAssert.Contains(json, "\"cpuUsagePercent\":18");
        StringAssert.Contains(json, "\"gpuUsagePercent\":36");
        StringAssert.Contains(json, "\"availableGb\":42.6");
        StringAssert.Contains(json, "\"cpuPackagePowerWatts\":88");
        StringAssert.Contains(json, "\"currentOutput\":\"Not connected\"");
    }
}
