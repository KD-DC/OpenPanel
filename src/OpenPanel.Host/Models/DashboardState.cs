namespace OpenPanel.Host.Models;

public sealed record DashboardState(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    AdvancedTelemetrySummary Advanced,
    StorageSummary Storage,
    MediaSummary Media,
    AudioSummary Audio,
    DisplaySummary Display);

public sealed record HardwareTelemetrySnapshot(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    AdvancedTelemetrySummary Advanced,
    StorageSummary Storage);

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

public sealed record StorageSummary(
    IReadOnlyList<StorageDeviceSummary> Devices);

public sealed record StorageDeviceSummary(
    string Name,
    double? UsedPercent,
    double? ActivityPercent,
    double? TemperatureCelsius,
    double? ReadMegabytesPerSecond,
    double? WriteMegabytesPerSecond);

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
