namespace OpenPanel.Host.Models;

public sealed record DashboardState(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    AdvancedTelemetrySummary Advanced,
    MotherboardSummary Motherboard,
    MediaSummary Media,
    AudioSummary Audio,
    DisplaySummary Display);

public sealed record HardwareTelemetrySnapshot(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    AdvancedTelemetrySummary Advanced,
    MotherboardSummary Motherboard);

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

public sealed record MotherboardSummary(
    IReadOnlyList<NamedSensorSummary> Temperatures,
    IReadOnlyList<NamedSensorSummary> Fans,
    IReadOnlyList<NamedSensorSummary> Voltages,
    IReadOnlyList<NamedSensorSummary> Power);

public sealed record NamedSensorSummary(
    string Name,
    double Value);

public sealed record MediaSummary(
    string Source,
    string Title,
    string Artist,
    string Album,
    string? ArtworkDataUrl,
    bool IsPlaying,
    double PositionSeconds,
    double DurationSeconds,
    bool CanToggle,
    bool CanGoPrevious,
    bool CanGoNext,
    bool CanSeek);

public sealed record AudioSummary(
    string? CurrentOutputId,
    string CurrentOutput,
    int VolumePercent,
    bool IsMuted,
    int PeakLevelPercent,
    bool SetCommunicationsDevice,
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
