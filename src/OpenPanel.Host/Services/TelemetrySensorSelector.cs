using LibreHardwareMonitor.Hardware;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

internal readonly record struct TelemetrySensorReading(
    HardwareType HardwareType,
    string HardwareId,
    string Name,
    SensorType SensorType,
    double Value);

internal static class TelemetrySensorSelector
{
    private static readonly string[] CpuTemperatureNames =
    [
        "CPU Package",
        "CPU (Tctl/Tdie)",
        "CPU Die (average)",
        "Core Average"
    ];

    private static readonly string[] GpuLoadNames =
    [
        "GPU Core",
        "GPU Utilization",
        "D3D 3D"
    ];

    private static readonly string[] GpuTemperatureNames =
    [
        "GPU Core",
        "GPU Temperature"
    ];

    public static double SelectCpuLoad(IEnumerable<TelemetrySensorReading> readings)
    {
        var cpuReadings = readings.Where(reading =>
            reading.HardwareType == HardwareType.Cpu &&
            reading.SensorType == SensorType.Load);

        var total = FindExact(cpuReadings, "CPU Total");
        if (total.HasValue)
        {
            return ClampPercent(total.Value);
        }

        var coreValues = cpuReadings
            .Where(reading => reading.Name.StartsWith("CPU Core", StringComparison.OrdinalIgnoreCase))
            .Select(reading => reading.Value)
            .ToArray();

        return coreValues.Length == 0 ? 0 : ClampPercent(coreValues.Average());
    }

    public static double? SelectCpuTemperature(IEnumerable<TelemetrySensorReading> readings)
    {
        var temperatures = readings.Where(reading =>
            reading.HardwareType == HardwareType.Cpu &&
            reading.SensorType == SensorType.Temperature &&
            IsValidTemperature(reading.Value));

        return FindPreferred(temperatures, CpuTemperatureNames) ??
            temperatures.Select(reading => (double?)reading.Value).Max();
    }

    public static GpuSummary SelectGpu(IEnumerable<TelemetrySensorReading> readings)
    {
        var gpu = readings
            .Where(reading => IsGpu(reading.HardwareType))
            .GroupBy(reading => reading.HardwareId)
            .OrderBy(group => GpuTypePriority(group.First().HardwareType))
            .ThenByDescending(group => FindExact(group, "GPU Memory Total") ?? 0)
            .FirstOrDefault();

        if (gpu is null)
        {
            return new GpuSummary(0, null, 0, 0);
        }

        var loadReadings = gpu.Where(reading => reading.SensorType == SensorType.Load);
        var load = FindPreferred(loadReadings, GpuLoadNames) ??
            loadReadings
                .Where(reading => !IsSecondaryGpuLoad(reading.Name))
                .Select(reading => (double?)reading.Value)
                .Max() ??
            0;

        var temperatures = gpu.Where(reading =>
            reading.SensorType == SensorType.Temperature &&
            IsValidTemperature(reading.Value));
        var temperature = FindPreferred(temperatures, GpuTemperatureNames) ??
            temperatures.Select(reading => (double?)reading.Value).Max();

        var memoryUsed = FindMemoryGigabytes(
            gpu,
            "GPU Memory Used",
            "D3D Dedicated Memory Used");
        var memoryTotal = FindMemoryGigabytes(gpu, "GPU Memory Total");

        return new GpuSummary(
            ClampPercent(load),
            temperature,
            Math.Max(0, memoryUsed ?? 0),
            Math.Max(0, memoryTotal ?? 0));
    }

    private static double? FindMemoryGigabytes(
        IEnumerable<TelemetrySensorReading> readings,
        params string[] names)
    {
        foreach (var name in names)
        {
            var reading = readings.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase) &&
                candidate.SensorType is SensorType.SmallData or SensorType.Data);

            if (reading != default)
            {
                return reading.SensorType == SensorType.SmallData
                    ? reading.Value / 1024d
                    : reading.Value;
            }
        }

        return null;
    }

    private static double? FindPreferred(
        IEnumerable<TelemetrySensorReading> readings,
        IEnumerable<string> preferredNames)
    {
        var materialized = readings.ToArray();
        foreach (var name in preferredNames)
        {
            var exact = FindExact(materialized, name);
            if (exact.HasValue)
            {
                return exact;
            }
        }

        return null;
    }

    private static double? FindExact(
        IEnumerable<TelemetrySensorReading> readings,
        string name)
    {
        foreach (var reading in readings)
        {
            if (string.Equals(reading.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return reading.Value;
            }
        }

        return null;
    }

    private static bool IsGpu(HardwareType hardwareType)
    {
        return hardwareType is
            HardwareType.GpuNvidia or
            HardwareType.GpuAmd or
            HardwareType.GpuIntel;
    }

    private static int GpuTypePriority(HardwareType hardwareType)
    {
        return hardwareType == HardwareType.GpuIntel ? 1 : 0;
    }

    private static bool IsSecondaryGpuLoad(string name)
    {
        return name.Contains("Memory", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Power", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Video", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Bus", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Fan", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidTemperature(double value)
    {
        return value is > 0 and < 150;
    }

    private static double ClampPercent(double value)
    {
        return Math.Clamp(value, 0, 100);
    }
}
