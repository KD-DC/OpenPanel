using LibreHardwareMonitor.Hardware;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

internal readonly record struct TelemetrySensorReading(
    HardwareType HardwareType,
    string HardwareId,
    string Name,
    SensorType SensorType,
    double Value,
    string HardwareName = "");

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

    private static readonly string[] CpuPowerNames =
    [
        "CPU Package",
        "Package",
        "CPU Cores"
    ];

    private static readonly string[] GpuPowerNames =
    [
        "GPU Power",
        "GPU Package",
        "GPU ASIC Power"
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
        var gpu = SelectPrimaryGpu(readings);

        if (gpu is null)
        {
            return new GpuSummary(0, null, 0, 0, null, null);
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
            Math.Max(0, memoryTotal ?? 0),
            PositiveOrNull(FindPreferred(
                gpu.Where(reading => reading.SensorType == SensorType.Power),
                GpuPowerNames)),
            gpu.Where(reading => reading.SensorType == SensorType.Fan)
                .Select(reading => (double?)reading.Value)
                .Max());
    }

    public static AdvancedTelemetrySummary SelectAdvanced(
        IEnumerable<TelemetrySensorReading> readings,
        MemorySummary memory)
    {
        var materialized = readings.ToArray();
        var cpu = materialized.Where(reading => reading.HardwareType == HardwareType.Cpu);
        var cpuCoreClocks = cpu
            .Where(reading =>
                reading.SensorType == SensorType.Clock &&
                reading.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) &&
                reading.Value > 0)
            .Select(reading => reading.Value)
            .ToArray();
        var gpu = SelectPrimaryGpu(materialized);

        return new AdvancedTelemetrySummary(
            SelectMemory(materialized, memory),
            cpuCoreClocks.Length == 0 ? null : cpuCoreClocks.Average(),
            PositiveOrNull(FindPreferred(
                cpu.Where(reading => reading.SensorType == SensorType.Power),
                CpuPowerNames)),
            gpu is null ? null : PositiveOrNull(FindExactByType(gpu, "GPU Core", SensorType.Clock)),
            gpu is null ? null : PositiveOrNull(FindExactByType(gpu, "GPU Memory", SensorType.Clock)),
            gpu?.Where(reading => reading.SensorType == SensorType.Control)
                .Select(reading => (double?)ClampPercent(reading.Value))
                .Max(),
            gpu is null ? null : FindPreferred(
                gpu.Where(reading =>
                    reading.SensorType == SensorType.Temperature &&
                    IsValidTemperature(reading.Value)),
                ["GPU Hot Spot", "GPU Hotspot"]),
            gpu is null ? null : FindPreferred(
                gpu.Where(reading =>
                    reading.SensorType == SensorType.Temperature &&
                    IsValidTemperature(reading.Value)),
                ["GPU Memory Junction", "GPU Memory"]));
    }

    public static StorageSummary SelectStorage(
        IEnumerable<TelemetrySensorReading> readings)
    {
        var devices = readings
            .Where(reading => reading.HardwareType == HardwareType.Storage)
            .GroupBy(reading => reading.HardwareId)
            .Select(group =>
            {
                var deviceReadings = group.ToArray();
                var usedPercent =
                    FindExactByType(deviceReadings, "Used Space", SensorType.Load) ??
                    FindExactByType(deviceReadings, "Used Space", SensorType.Level);
                var activityPercent =
                    FindExactByType(deviceReadings, "Total Activity", SensorType.Load);
                var temperature = FindPreferred(
                    deviceReadings.Where(reading =>
                        reading.SensorType == SensorType.Temperature &&
                        IsValidTemperature(reading.Value)),
                    ["Temperature", "Drive Temperature", "Composite Temperature"]);
                var readRate = FindExactByType(
                    deviceReadings,
                    "Read Rate",
                    SensorType.Throughput);
                var writeRate = FindExactByType(
                    deviceReadings,
                    "Write Rate",
                    SensorType.Throughput);

                return new StorageDeviceSummary(
                    deviceReadings[0].HardwareName,
                    usedPercent.HasValue ? ClampPercent(usedPercent.Value) : null,
                    activityPercent.HasValue ? ClampPercent(activityPercent.Value) : null,
                    temperature,
                    BytesPerSecondToMegabytesPerSecond(readRate),
                    BytesPerSecondToMegabytesPerSecond(writeRate));
            })
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new StorageSummary(devices);
    }

    private static MemorySummary SelectMemory(
        IEnumerable<TelemetrySensorReading> readings,
        MemorySummary fallback)
    {
        var memory = readings
            .Where(reading => reading.HardwareType == HardwareType.Memory)
            .ToArray();
        var used = PositiveOrNull(FindExactByType(memory, "Used Memory", SensorType.Data)) ??
            fallback.UsedGb;
        var available = PositiveOrNull(FindExactByType(memory, "Available Memory", SensorType.Data)) ??
            fallback.AvailableGb;
        var virtualUsed = PositiveOrNull(FindExactByType(memory, "Used Virtual Memory", SensorType.Data)) ??
            fallback.VirtualUsedGb;
        var virtualAvailable = PositiveOrNull(
            FindExactByType(memory, "Available Virtual Memory", SensorType.Data));
        var total = used + available;

        return new MemorySummary(
            used,
            available,
            total,
            total > 0 ? ClampPercent(used / total * 100) : fallback.LoadPercent,
            virtualUsed,
            virtualAvailable.HasValue
                ? virtualUsed + virtualAvailable.Value
                : fallback.VirtualTotalGb);
    }

    private static IGrouping<string, TelemetrySensorReading>? SelectPrimaryGpu(
        IEnumerable<TelemetrySensorReading> readings)
    {
        return readings
            .Where(reading => IsGpu(reading.HardwareType))
            .GroupBy(reading => reading.HardwareId)
            .OrderBy(group => GpuTypePriority(group.First().HardwareType))
            .ThenByDescending(group => FindExact(group, "GPU Memory Total") ?? 0)
            .FirstOrDefault();
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

    private static double? FindExactByType(
        IEnumerable<TelemetrySensorReading> readings,
        string name,
        SensorType sensorType)
    {
        return readings
            .Where(reading => reading.SensorType == sensorType)
            .Select(reading =>
                string.Equals(reading.Name, name, StringComparison.OrdinalIgnoreCase)
                    ? (double?)reading.Value
                    : null)
            .FirstOrDefault(value => value.HasValue);
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

    private static double? BytesPerSecondToMegabytesPerSecond(double? value)
    {
        return value is >= 0 ? value / 1_000_000d : null;
    }

    private static double? PositiveOrNull(double? value)
    {
        return value is > 0 ? value : null;
    }

    private static double ClampPercent(double value)
    {
        return Math.Clamp(value, 0, 100);
    }
}
