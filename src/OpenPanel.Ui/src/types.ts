export interface HostToUiMessage {
  type: "state:update";
  payload: DashboardState;
}

export interface UiToHostMessage<TPayload = unknown> {
  type: "command:audio.select" | "command:media.toggle" | "command:system.ready";
  payload?: TPayload;
}

export interface DashboardState {
  telemetry: TelemetrySummary;
  gpu: GpuSummary;
  media: MediaSummary;
  audio: AudioSummary;
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
}

export interface MediaSummary {
  source: string;
  title: string;
  artist: string;
  album: string;
  isPlaying: boolean;
  positionSeconds: number;
  durationSeconds: number;
}

export interface AudioSummary {
  currentOutput: string;
  volumePercent: number;
  isMuted: boolean;
  outputs: AudioOutputSummary[];
}

export interface AudioOutputSummary {
  id: string;
  name: string;
  isDefault: boolean;
}

export interface DisplaySummary {
  name: string;
  left: number;
  top: number;
  width: number;
  height: number;
  isPrimary: boolean;
}
