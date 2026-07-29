using LibreHardwareMonitor.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}
