using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class GamingPerformanceServiceTests
{
    [TestMethod]
    public void ParseCsvHandlesQuotedApplicationNames()
    {
        var values = GamingPerformanceAccumulator.ParseCsv(
            "\"Game, The.exe\",42,16.7");

        CollectionAssert.AreEqual(
            new[] { "Game, The.exe", "42", "16.7" },
            values.ToArray());
    }

    [TestMethod]
    public void AccumulatorCalculatesMetricsForForegroundWorkload()
    {
        var accumulator = new GamingPerformanceAccumulator();
        accumulator.AcceptLine("Application,MsBetweenPresents,MsGPUBusy");
        for (var index = 0; index < 60; index++)
        {
            accumulator.AcceptLine("game.exe,16.67,8.4");
        }

        var snapshot = accumulator.GetSnapshot();

        Assert.AreEqual("game.exe", snapshot.Application);
        Assert.AreEqual(60, snapshot.Fps);
        Assert.AreEqual(16.67, snapshot.FrameTimeMs!.Value, 0.001);
        Assert.AreEqual(8.4, snapshot.GpuBusyMs!.Value, 0.001);
    }

    [TestMethod]
    public void CollectorExitStatusExplainsMissingWindowsPermission()
    {
        var status = GamingPerformanceService.CollectorExitStatus(
            "error: access denied. Add the account to Performance Log Users.");

        Assert.AreEqual("Windows performance access required", status);
    }
}
