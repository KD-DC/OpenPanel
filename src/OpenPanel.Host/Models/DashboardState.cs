namespace OpenPanel.Host.Models;

public sealed record DashboardState(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    MediaSummary Media,
    AudioSummary Audio,
    DisplaySummary Display);

public sealed record HardwareTelemetrySnapshot(
    TelemetrySummary Telemetry,
    GpuSummary Gpu);

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
    double VramTotalGb);

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
