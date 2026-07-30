namespace OpenPanel.Host.Models;

public sealed record DashboardState(
    TelemetrySummary Telemetry,
    GpuSummary Gpu,
    AdvancedTelemetrySummary Advanced,
    StorageSummary Storage,
    NetworkQualitySummary Network,
    ProcessUsageSummary Processes,
    PeripheralBatterySummary Peripherals,
    GamingPerformanceSummary Gaming,
    MediaSummary Media,
    AudioSummary Audio,
    WeatherSummary Weather,
    AppearanceSummary Appearance,
    WidgetConfigurationSummary Widgets,
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

public sealed record NetworkQualitySummary(
    bool IsActive,
    bool IsAvailable,
    string Status,
    string InterfaceName,
    string ConnectionType,
    string LocalAddress,
    double? LinkSpeedMbps,
    double? LatencyMs,
    double? JitterMs,
    double? PacketLossPercent,
    string Target,
    NetworkApplicationTrafficSummary ApplicationTraffic);

public sealed record NetworkApplicationTrafficSummary(
    bool IsActive,
    bool IsAvailable,
    bool RequiresPermission,
    string Status,
    IReadOnlyList<NetworkApplicationSummary> Applications);

public sealed record NetworkApplicationSummary(
    int ProcessId,
    string Name,
    double UploadMbps,
    double DownloadMbps);

public sealed record ProcessUsageSummary(
    bool IsActive,
    string Status,
    IReadOnlyList<ProcessUsageApplicationSummary> TopCpu,
    IReadOnlyList<ProcessUsageApplicationSummary> TopMemory);

public sealed record ProcessUsageApplicationSummary(
    string Name,
    double CpuPercent,
    double MemoryMegabytes);

public sealed record PeripheralBatterySummary(
    IReadOnlyList<PeripheralBatteryDeviceSummary> Devices,
    DateTimeOffset? UpdatedAt);

public sealed record PeripheralBatteryDeviceSummary(
    string Id,
    string Name,
    string Category,
    int? BatteryPercent,
    bool? IsCharging,
    bool IsConnected,
    string Source,
    string? BatteryState = null);

public sealed record GamingPerformanceSummary(
    bool IsActive,
    bool CollectorAvailable,
    string Status,
    string Application,
    double? Fps,
    double? FrameTimeMs,
    double? OnePercentLowFps,
    double? GpuBusyMs,
    int StutterCount,
    DateTimeOffset? StartedAt);

public sealed record MediaSummary(
    string Source,
    string Title,
    string Artist,
    string Album,
    string AlbumArtist,
    string Subtitle,
    IReadOnlyList<string> Genres,
    int TrackNumber,
    int AlbumTrackCount,
    string PlaybackStatus,
    string PlaybackType,
    bool? IsShuffleActive,
    string RepeatMode,
    double? PlaybackRate,
    string? ArtworkDataUrl,
    bool IsPlaying,
    double PositionSeconds,
    double DurationSeconds,
    bool CanToggle,
    bool CanGoPrevious,
    bool CanGoNext,
    bool CanShuffle,
    bool CanSeek);

public sealed record AudioSummary(
    string? CurrentOutputId,
    string CurrentOutput,
    int VolumePercent,
    bool IsMuted,
    int PeakLevelPercent,
    bool SetCommunicationsDevice,
    IReadOnlyList<AudioOutputSummary> Outputs,
    string? CurrentInputId,
    string CurrentInput,
    int InputVolumePercent,
    bool IsInputMuted,
    int InputPeakLevelPercent,
    IReadOnlyList<AudioInputSummary> Inputs,
    IReadOnlyList<AudioSessionSummary> Sessions);

public sealed record AudioOutputSummary(
    string Id,
    string Name,
    bool IsDefault);

public sealed record AudioInputSummary(
    string Id,
    string Name,
    bool IsDefault);

public sealed record AudioSessionSummary(
    string Id,
    string Name,
    int VolumePercent,
    bool IsMuted,
    int PeakLevelPercent);

public sealed record WeatherSummary(
    string Location,
    bool IsAvailable,
    bool IsStale,
    string Status,
    double? CurrentTemperatureFahrenheit,
    double? ApparentTemperatureFahrenheit,
    double? HumidityPercent,
    double? WindSpeedMph,
    int? WeatherCode,
    IReadOnlyList<HourlyForecastSummary> Hourly,
    IReadOnlyList<DailyForecastSummary> Daily,
    AirQualitySummary AirQuality,
    DateTimeOffset? UpdatedAt);

public sealed record HourlyForecastSummary(
    DateTime Time,
    double? TemperatureFahrenheit,
    int? WeatherCode,
    double? PrecipitationProbabilityPercent);

public sealed record DailyForecastSummary(
    DateOnly Date,
    double? HighFahrenheit,
    double? LowFahrenheit,
    int? WeatherCode,
    double? PrecipitationProbabilityPercent);

public sealed record AirQualitySummary(
    double? UsAqi,
    string Category,
    double? Pm25,
    double? Pm10,
    double? Ozone);

public sealed record AppearanceSummary(string Theme);

public sealed record WidgetConfigurationSummary(
    IReadOnlyList<WidgetVisibilitySummary> Items);

public sealed record WidgetVisibilitySummary(
    string Id,
    string Label,
    bool IsVisible);

public sealed record DisplaySummary(
    string Name,
    int Left,
    int Top,
    int Width,
    int Height,
    bool IsPrimary);
