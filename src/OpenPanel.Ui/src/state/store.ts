import type { DashboardState } from "../types";

export const initialState: DashboardState = {
  telemetry: {
    cpuUsagePercent: 0,
    cpuTemperatureCelsius: null,
    memoryUsedGb: 0,
    memoryTotalGb: 0,
    networkUploadMbps: 0,
    networkDownloadMbps: 0
  },
  gpu: {
    gpuUsagePercent: 0,
    gpuTemperatureCelsius: null,
    vramUsedGb: 0,
    vramTotalGb: 0
  },
  media: {
    source: "No session",
    title: "Waiting for host",
    artist: "OpenPanel",
    album: "",
    isPlaying: false,
    positionSeconds: 0,
    durationSeconds: 0
  },
  audio: {
    currentOutput: "Unavailable",
    volumePercent: 0,
    isMuted: false,
    outputs: []
  },
  display: {
    name: "Unknown",
    left: 0,
    top: 0,
    width: 1920,
    height: 550,
    isPrimary: false
  }
};
