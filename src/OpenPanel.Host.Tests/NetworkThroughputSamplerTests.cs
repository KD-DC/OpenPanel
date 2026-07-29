using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class NetworkThroughputSamplerTests
{
    [TestMethod]
    public void BytesToMegabitsPerSecondUsesDecimalNetworkUnits()
    {
        var result = NetworkThroughputSampler.BytesToMegabitsPerSecond(
            1_000_000,
            TimeSpan.FromSeconds(2));

        Assert.AreEqual(4, result, 0.001);
    }

    [TestMethod]
    public void BytesToMegabitsPerSecondRejectsEmptyIntervals()
    {
        Assert.AreEqual(
            0,
            NetworkThroughputSampler.BytesToMegabitsPerSecond(
                1_000_000,
                TimeSpan.Zero));
    }
}
