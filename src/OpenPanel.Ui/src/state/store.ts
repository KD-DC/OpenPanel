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
    vramTotalGb: 0,
    gpuPowerWatts: null,
    gpuFanRpm: null
  },
  advanced: {
    memory: {
      usedGb: 0,
      availableGb: 0,
      totalGb: 0,
      loadPercent: 0,
      virtualUsedGb: 0,
      virtualTotalGb: 0
    },
    cpuAverageClockMhz: null,
    cpuPackagePowerWatts: null,
    gpuCoreClockMhz: null,
    gpuMemoryClockMhz: null,
    gpuFanPercent: null,
    gpuHotSpotTemperatureCelsius: null,
    gpuMemoryTemperatureCelsius: null
  },
  media: {
    source: "No session",
    title: "Waiting for host",
    artist: "OpenPanel",
    album: "",
    artworkDataUrl: null,
    isPlaying: false,
    positionSeconds: 0,
    durationSeconds: 0,
    canToggle: false,
    canGoPrevious: false,
    canGoNext: false,
    canSeek: false
  },
  audio: {
    currentOutputId: null,
    currentOutput: "Unavailable",
    volumePercent: 0,
    isMuted: false,
    peakLevelPercent: 0,
    setCommunicationsDevice: true,
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
