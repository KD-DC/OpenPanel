using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;
using System.Xml.Linq;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class PeripheralBatteryServiceTests
{
    [DataTestMethod]
    [DataRow("MX Master 3S Mouse", "Mouse")]
    [DataRow("MX Keys Keyboard", "Keyboard")]
    [DataRow("Bose NC 700 Headphones", "Headphones")]
    [DataRow("Xbox Wireless Controller", "Controller")]
    public void CategoryRecognizesSupportedPeripherals(
        string name,
        string expected)
    {
        Assert.AreEqual(expected, PeripheralBatteryService.Category(name));
    }

    [TestMethod]
    public void LogitechVoltageCurveIsBounded()
    {
        Assert.AreEqual(100, LogitechHidBatteryReader.VoltageToPercent(4200));
        Assert.AreEqual(0, LogitechHidBatteryReader.VoltageToPercent(3400));
    }

    [TestMethod]
    public void LogitechOptionsFlowDeviceRegistersWithoutBattery()
    {
        var device = LogitechOptionsDeviceReader.Parse(
            XDocument.Parse("""<device type="mouse" id="438307" />"""));

        Assert.IsNotNull(device);
        Assert.AreEqual("Mouse", device.Category);
        Assert.AreEqual("Logitech mouse", device.Name);
        Assert.IsNull(device.BatteryPercent);
        Assert.AreEqual("Logi Options+", device.Source);
    }
}
