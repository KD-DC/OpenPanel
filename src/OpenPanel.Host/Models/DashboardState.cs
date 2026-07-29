namespace OpenPanel.Host.Models;

public sealed record DashboardState(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    AdvancedTelemetrySummary Advanced,
    MediaSummary Media,
    AudioSummary Audio,
    DisplaySummary Display);

public sealed record HardwareTelemetrySnapshot(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    AdvancedTelemetrySummary Advanced);

public sealed record TelemetrySummary(
    double CpuUsagePercent,
    double? CpuTemperatureCelsius,
    double MemoryUsedGb,
    double MemoryTotalGb,
    double NetworkUploadMbps,
    double NetworkDownloadMbps);

public sealed record GpuSummary(
    double GpuUsagePercent,
    double? GpuTemperatureCelsius,
    double VramUsedGb,
    double VramTotalGb,
    double? GpuPowerWatts,
    double? GpuFanRpm);

public sealed record AdvancedTelemetrySummary(
    MemorySummary Memory,
    double? CpuAverageClockMhz,
    double? CpuPackagePowerWatts,
    double? GpuCoreClockMhz,
    double? GpuMemoryClockMhz,
    double? GpuFanPercent,
    double? GpuHotSpotTemperatureCelsius,
    double? GpuMemoryTemperatureCelsius);

public sealed record MemorySummary(
    double UsedGb,
    double AvailableGb,
    double TotalGb,
    double LoadPercent,
    double VirtualUsedGb,
    double VirtualTotalGb);

public sealed record MediaSummary(
    string Source,
    string Title,
    string Artist,
    string Album,
    bool IsPlaying,
    double PositionSeconds,
    double DurationSeconds);

public sealed record AudioSummary(
    string CurrentOutput,
    int VolumePercent,
    bool IsMuted,
    IReadOnlyList<AudioOutputSummary> Outputs);

public sealed record AudioOutputSummary(
    string Id,
    string Name,
    bool IsDefault);

public sealed record DisplaySummary(
    string Name,
    int Left,
    int Top,
    int Width,
    int Height,
    bool IsPrimary);
