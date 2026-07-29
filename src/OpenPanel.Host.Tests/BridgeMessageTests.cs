using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Messaging;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class BridgeMessageTests
{
    [TestMethod]
    public void StateUpdateSerializesWithCamelCaseFields()
    {
        var payload = new DashboardStateProvider().CreateSampleState();
        var message = new HostToUiMessage("state:update", payload);

        var json = JsonSerializer.Serialize(message, MessageJson.Options);

        StringAssert.Contains(json, "\"type\":\"state:update\"");
        StringAssert.Contains(json, "\"cpuUsagePercent\":18");
        StringAssert.Contains(json, "\"currentOutput\":\"Desk Speakers\"");
    }
}
