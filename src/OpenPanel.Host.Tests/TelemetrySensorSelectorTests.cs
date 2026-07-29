using LibreHardwareMonitor.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Models;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class TelemetrySensorSelectorTests
{
    [TestMethod]
    public void SelectCpuReadingsPrefersTotalAndPackageSensors()
    {
        TelemetrySensorReading[] readings =
        [
            new(HardwareType.Cpu, "cpu/0", "CPU Core #1", SensorType.Load, 25),
            new(HardwareType.Cpu, "cpu/0", "CPU Total", SensorType.Load, 42.5),
            new(HardwareType.Cpu, "cpu/0", "CPU Core #1", SensorType.Temperature, 61),
            new(HardwareType.Cpu, "cpu/0", "CPU Package", SensorType.Temperature, 54)
        ];

        Assert.AreEqual(42.5, TelemetrySensorSelector.SelectCpuLoad(readings), 0.01);
        Assert.AreEqual(54, TelemetrySensorSelector.SelectCpuTemperature(readings));
    }

    [TestMethod]
    public void SelectGpuPrefersDiscreteAdapterAndConvertsMemoryMegabytes()
    {
        TelemetrySensorReading[] readings =
        [
            new(HardwareType.GpuIntel, "gpu/intel/0", "GPU Core", SensorType.Load, 90),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Core", SensorType.Load, 34),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Core", SensorType.Temperature, 49),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Memory Used", SensorType.SmallData, 6144),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Memory Total", SensorType.SmallData, 16384)
        ];

        var gpu = TelemetrySensorSelector.SelectGpu(readings);

        Assert.AreEqual(34, gpu.GpuUsagePercent, 0.01);
        Assert.AreEqual(49, gpu.GpuTemperatureCelsius);
        Assert.AreEqual(6, gpu.VramUsedGb, 0.01);
        Assert.AreEqual(16, gpu.VramTotalGb, 0.01);
    }

    [TestMethod]
    public void SelectAdvancedReadsCpuGpuPowerClockFanAndThermalSensors()
    {
        TelemetrySensorReading[] readings =
        [
            new(HardwareType.Cpu, "cpu/0", "CPU Core #1", SensorType.Clock, 4100),
            new(HardwareType.Cpu, "cpu/0", "CPU Core #2", SensorType.Clock, 4300),
            new(HardwareType.Cpu, "cpu/0", "CPU Package", SensorType.Power, 86),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Core", SensorType.Clock, 2505),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Memory", SensorType.Clock, 10501),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Fan", SensorType.Control, 47),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Hot Spot", SensorType.Temperature, 68),
            new(HardwareType.GpuNvidia, "gpu/nvidia/0", "GPU Memory Junction", SensorType.Temperature, 74)
        ];
        var memory = new MemorySummary(20, 44, 64, 31.25, 26, 72);

        var advanced = TelemetrySensorSelector.SelectAdvanced(readings, memory);

        Assert.AreEqual(4200, advanced.CpuAverageClockMhz);
        Assert.AreEqual(86, advanced.CpuPackagePowerWatts);
        Assert.AreEqual(2505, advanced.GpuCoreClockMhz);
        Assert.AreEqual(10501, advanced.GpuMemoryClockMhz);
        Assert.AreEqual(47, advanced.GpuFanPercent);
        Assert.AreEqual(68, advanced.GpuHotSpotTemperatureCelsius);
        Assert.AreEqual(74, advanced.GpuMemoryTemperatureCelsius);
    }

    [TestMethod]
    public void SelectAdvancedPrefersLibreHardwareMemoryReadings()
    {
        TelemetrySensorReading[] readings =
        [
            new(HardwareType.Memory, "ram", "Memory", SensorType.Load, 37.5),
            new(HardwareType.Memory, "ram", "Used Memory", SensorType.Data, 24),
            new(HardwareType.Memory, "ram", "Available Memory", SensorType.Data, 40),
            new(HardwareType.Memory, "ram", "Used Virtual Memory", SensorType.Data, 30),
            new(HardwareType.Memory, "ram", "Available Virtual Memory", SensorType.Data, 42)
        ];
        var fallback = new MemorySummary(20, 44, 64, 31.25, 26, 70);

        var advanced = TelemetrySensorSelector.SelectAdvanced(readings, fallback);

        Assert.AreEqual(24, advanced.Memory.UsedGb);
        Assert.AreEqual(40, advanced.Memory.AvailableGb);
        Assert.AreEqual(64, advanced.Memory.TotalGb);
        Assert.AreEqual(37.5, advanced.Memory.LoadPercent);
        Assert.AreEqual(30, advanced.Memory.VirtualUsedGb);
        Assert.AreEqual(72, advanced.Memory.VirtualTotalGb);
    }

    [TestMethod]
    public void SelectCpuLoadClampsInvalidPercentageRange()
    {
        TelemetrySensorReading[] readings =
        [
            new(HardwareType.Cpu, "cpu/0", "CPU Total", SensorType.Load, 104)
        ];

        Assert.AreEqual(100, TelemetrySensorSelector.SelectCpuLoad(readings));
    }

    [TestMethod]
    public void SelectCpuTemperatureIgnoresUnsupportedZeroReading()
    {
        TelemetrySensorReading[] readings =
        [
            new(HardwareType.Cpu, "cpu/0", "CPU Package", SensorType.Temperature, 0)
        ];

        Assert.IsNull(TelemetrySensorSelector.SelectCpuTemperature(readings));
    }

    [TestMethod]
    public void SelectStorageGroupsDevicesAndConvertsThroughput()
    {
        TelemetrySensorReading[] readings =
        [
            new(HardwareType.Storage, "storage/0", "Used Space", SensorType.Load, 63, "System NVMe"),
            new(HardwareType.Storage, "storage/0", "Total Activity", SensorType.Load, 18, "System NVMe"),
            new(HardwareType.Storage, "storage/0", "Temperature", SensorType.Temperature, 42, "System NVMe"),
            new(HardwareType.Storage, "storage/0", "Read Rate", SensorType.Throughput, 125_000_000, "System NVMe"),
            new(HardwareType.Storage, "storage/0", "Write Rate", SensorType.Throughput, 25_000_000, "System NVMe"),
            new(HardwareType.Storage, "storage/1", "Used Space", SensorType.Level, 40, "Archive SSD")
        ];

        var storage = TelemetrySensorSelector.SelectStorage(readings);

        Assert.HasCount(2, storage.Devices);
        var system = storage.Devices.Single(device => device.Name == "System NVMe");
        Assert.AreEqual(63, system.UsedPercent);
        Assert.AreEqual(18, system.ActivityPercent);
        Assert.AreEqual(42, system.TemperatureCelsius);
        Assert.AreEqual(125, system.ReadMegabytesPerSecond);
        Assert.AreEqual(25, system.WriteMegabytesPerSecond);
    }
}
