using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class NetworkApplicationTrafficServiceTests
{
    [TestMethod]
    public void CreateSnapshot_OrdersByCombinedTrafficAndCalculatesRates()
    {
        var counters = new Dictionary<int, NetworkTrafficCounters>
        {
            [10] = new("Browser", 250_000, 1_000_000),
            [20] = new(null, 500_000, 0)
        };

        var result = NetworkApplicationTrafficService.CreateSnapshot(
            counters,
            TimeSpan.FromSeconds(2),
            processId => processId == 20 ? "Uploader" : null);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Browser", result[0].Name);
        Assert.AreEqual(4, result[0].DownloadMbps, 0.001);
        Assert.AreEqual(1, result[0].UploadMbps, 0.001);
        Assert.AreEqual("Uploader", result[1].Name);
        Assert.AreEqual(2, result[1].UploadMbps, 0.001);
    }

    [TestMethod]
    public void CreateSnapshot_UsesPidWhenProcessNameCannotBeResolved()
    {
        var counters = new Dictionary<int, NetworkTrafficCounters>
        {
            [44] = new(null, 1, 0)
        };

        var result = NetworkApplicationTrafficService.CreateSnapshot(
            counters,
            TimeSpan.FromSeconds(1),
            _ => null);

        Assert.AreEqual("Process 44", result.Single().Name);
    }
}
