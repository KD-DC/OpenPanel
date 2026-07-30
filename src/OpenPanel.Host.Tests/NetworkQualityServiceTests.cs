using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class NetworkQualityServiceTests
{
    [TestMethod]
    public void CalculateMetricsIncludesLatencyJitterAndLoss()
    {
        var metrics = NetworkQualityService.CalculateMetrics(
        [
            new NetworkProbeSample(true, 10),
            new NetworkProbeSample(true, 14),
            new NetworkProbeSample(false, 0),
            new NetworkProbeSample(true, 12)
        ]);

        Assert.AreEqual(12, metrics.LatencyMs);
        Assert.AreEqual(3, metrics.JitterMs);
        Assert.AreEqual(25, metrics.PacketLossPercent);
    }
}
