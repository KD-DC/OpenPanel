using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class ProcessUsageServiceTests
{
    [TestMethod]
    public void CreateSummary_AggregatesProcessesByApplicationName()
    {
        var previous = new Dictionary<int, ProcessUsageSample>
        {
            [10] = new(10, "Browser", TimeSpan.FromSeconds(2), 100 * 1024 * 1024),
            [11] = new(11, "Browser", TimeSpan.FromSeconds(1), 150 * 1024 * 1024)
        };
        var current = new[]
        {
            new ProcessUsageSample(
                10,
                "Browser",
                TimeSpan.FromSeconds(2.4),
                120 * 1024 * 1024),
            new ProcessUsageSample(
                11,
                "Browser",
                TimeSpan.FromSeconds(1.4),
                180 * 1024 * 1024)
        };

        var result = ProcessUsageService.CreateSummary(
            current,
            previous,
            TimeSpan.FromSeconds(2),
            4);

        Assert.AreEqual(1, result.TopCpu.Count);
        Assert.AreEqual("Browser", result.TopCpu[0].Name);
        Assert.AreEqual(10, result.TopCpu[0].CpuPercent, 0.001);
        Assert.AreEqual(300, result.TopMemory[0].MemoryMegabytes, 0.001);
    }

    [TestMethod]
    public void CreateSummary_DoesNotAttributeCpuToNewProcesses()
    {
        var current = new[]
        {
            new ProcessUsageSample(
                22,
                "New app",
                TimeSpan.FromSeconds(3),
                64 * 1024 * 1024)
        };

        var result = ProcessUsageService.CreateSummary(
            current,
            new Dictionary<int, ProcessUsageSample>(),
            TimeSpan.FromSeconds(2),
            8);

        Assert.AreEqual(0, result.TopCpu.Count);
        Assert.AreEqual("New app", result.TopMemory.Single().Name);
    }

    [TestMethod]
    public void SetActiveFalse_ClearsRankingsAndReturnsInactiveState()
    {
        var service = new ProcessUsageService();

        service.SetActive(true);
        var active = service.GetSnapshot();
        service.SetActive(false);
        var inactive = service.GetSnapshot();

        Assert.IsTrue(active.IsActive);
        Assert.IsFalse(inactive.IsActive);
        Assert.AreEqual(0, inactive.TopCpu.Count);
        Assert.AreEqual(0, inactive.TopMemory.Count);
    }
}
