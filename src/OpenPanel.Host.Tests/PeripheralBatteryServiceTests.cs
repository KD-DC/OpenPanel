using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;
using System.Text.Json;

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
    public void LogitechOptionsAgentParsesBatteryCapableDevices()
    {
        using var document = JsonDocument.Parse("""
            {
              "deviceInfos": [
                {
                  "id": "dev00000002",
                  "displayName": "MX Master 3",
                  "deviceType": "MOUSE",
                  "state": "ACTIVE",
                  "capabilities": { "hasBatteryStatus": true }
                },
                {
                  "id": "dev00000000",
                  "displayName": "Logi Unifying receiver",
                  "deviceType": "RECEIVER",
                  "state": "PRESENT",
                  "capabilities": { "hasBatteryStatus": false }
                }
              ]
            }
            """);

        var devices = LogitechOptionsBatteryReader.ParseDevices(
            document.RootElement);

        Assert.HasCount(1, devices);
        Assert.AreEqual("MX Master 3", devices[0].Name);
        Assert.AreEqual("Mouse", devices[0].Category);
        Assert.IsTrue(devices[0].IsConnected);
    }

    [TestMethod]
    public void LogitechOptionsAgentParsesExactAndCoarseBatteryStates()
    {
        using var valid = JsonDocument.Parse("""
            {
              "deviceId": "dev00000002",
              "percentage": 74,
              "level": "GOOD",
              "charging": false
            }
            """);
        using var coarseOnly = JsonDocument.Parse("""
            {
              "deviceId": "dev00000001",
              "level": "LOW_BATTERY"
            }
            """);

        var battery = LogitechOptionsBatteryReader.ParseBattery(
            valid.RootElement);
        var coarseBattery = LogitechOptionsBatteryReader.ParseBattery(
            coarseOnly.RootElement);

        Assert.IsNotNull(battery);
        Assert.AreEqual(74, battery.Percent);
        Assert.AreEqual("Good", battery.State);
        Assert.IsFalse(battery.IsCharging);
        Assert.IsNotNull(coarseBattery);
        Assert.IsNull(coarseBattery.Percent);
        Assert.AreEqual("Low battery", coarseBattery.State);
    }
}
