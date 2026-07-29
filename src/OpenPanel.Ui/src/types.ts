export interface HostToUiMessage {
  type: "state:update";
  payload: DashboardState;
}

export interface UiToHostMessage<TPayload = unknown> {
  type:
    | "command:audio.select"
    | "command:audio.volume"
    | "command:audio.mute"
    | "command:audio.expanded"
    | "command:audio.input.select"
    | "command:audio.input.volume"
    | "command:audio.input.mute"
    | "command:audio.session.volume"
    | "command:audio.session.mute"
    | "command:media.toggle"
    | "command:media.previous"
    | "command:media.next"
    | "command:media.shuffle"
    | "command:media.seek"
    | "command:system.ready";
  payload?: TPayload;
}

export interface DashboardState {
  telemetry: TelemetrySummary;
  gpu: GpuSummary;
  advanced: AdvancedTelemetrySummary;
  storage: StorageSummary;
  media: MediaSummary;
  audio: AudioSummary;
  weather: WeatherSummary;
  appearance: AppearanceSummary;
  display: DisplaySummary;
}

export interface TelemetrySummary {
  cpuUsagePercent: number;
  cpuTemperatureCelsius: number | null;
  memoryUsedGb: number;
  memoryTotalGb: number;
  networkUploadMbps: number;
  networkDownloadMbps: number;
}

export interface GpuSummary {
  gpuUsagePercent: number;
  gpuTemperatureCelsius: number | null;
  vramUsedGb: number;
  vramTotalGb: number;
  gpuPowerWatts: number | null;
  gpuFanRpm: number | null;
}

export interface AdvancedTelemetrySummary {
  memory: MemorySummary;
  cpuAverageClockMhz: number | null;
  cpuPackagePowerWatts: number | null;
  gpuCoreClockMhz: number | null;
  gpuMemoryClockMhz: number | null;
  gpuFanPercent: number | null;
  gpuHotSpotTemperatureCelsius: number | null;
  gpuMemoryTemperatureCelsius: number | null;
}

export interface MemorySummary {
  usedGb: number;
  availableGb: number;
  totalGb: number;
  loadPercent: number;
  virtualUsedGb: number;
  virtualTotalGb: number;
}

export interface StorageSummary {
  devices: StorageDeviceSummary[];
}

export interface StorageDeviceSummary {
  name: string;
  usedPercent: number | null;
  activityPercent: number | null;
  temperatureCelsius: number | null;
  readMegabytesPerSecond: number | null;
  writeMegabytesPerSecond: number | null;
}

export interface MediaSummary {
  source: string;
  title: string;
  artist: string;
  album: string;
  albumArtist: string;
  subtitle: string;
  genres: string[];
  trackNumber: number;
  albumTrackCount: number;
  playbackStatus: string;
  playbackType: string;
  isShuffleActive: boolean | null;
  repeatMode: string;
  playbackRate: number | null;
  artworkDataUrl: string | null;
  isPlaying: boolean;
  positionSeconds: number;
  durationSeconds: number;
  canToggle: boolean;
  canGoPrevious: boolean;
  canGoNext: boolean;
  canShuffle: boolean;
  canSeek: boolean;
}

export interface AudioSummary {
  currentOutputId: string | null;
  currentOutput: string;
  volumePercent: number;
  isMuted: boolean;
  peakLevelPercent: number;
  setCommunicationsDevice: boolean;
  outputs: AudioOutputSummary[];
  currentInputId: string | null;
  currentInput: string;
  inputVolumePercent: number;
  isInputMuted: boolean;
  inputPeakLevelPercent: number;
  inputs: AudioInputSummary[];
  sessions: AudioSessionSummary[];
}

export interface AudioOutputSummary {
  id: string;
  name: string;
  isDefault: boolean;
}

export interface AudioInputSummary {
  id: string;
  name: string;
  isDefault: boolean;
}

export interface AudioSessionSummary {
  id: string;
  name: string;
  volumePercent: number;
  isMuted: boolean;
  peakLevelPercent: number;
}

export interface WeatherSummary {
  location: string;
  isAvailable: boolean;
  isStale: boolean;
  status: string;
  currentTemperatureFahrenheit: number | null;
  apparentTemperatureFahrenheit: number | null;
  humidityPercent: number | null;
  windSpeedMph: number | null;
  weatherCode: number | null;
  hourly: HourlyForecastSummary[];
  daily: DailyForecastSummary[];
  airQuality: AirQualitySummary;
  updatedAt: string | null;
}

export interface HourlyForecastSummary {
  time: string;
  temperatureFahrenheit: number | null;
  weatherCode: number | null;
  precipitationProbabilityPercent: number | null;
}

export interface DailyForecastSummary {
  date: string;
  highFahrenheit: number | null;
  lowFahrenheit: number | null;
  weatherCode: number | null;
  precipitationProbabilityPercent: number | null;
}

export interface AirQualitySummary {
  usAqi: number | null;
  category: string;
  pm25: number | null;
  pm10: number | null;
  ozone: number | null;
}

export interface AppearanceSummary {
  theme: "current" | "mediaOled";
}

export interface DisplaySummary {
  name: string;
  left: number;
  top: number;
  width: number;
  height: number;
  isPrimary: boolean;
}
