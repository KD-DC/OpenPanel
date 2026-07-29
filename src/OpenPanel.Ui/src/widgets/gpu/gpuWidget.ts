import type { GpuSummary } from "../../types";

export function renderGpuWidget(state: GpuSummary): string {
  return `
    <section class="widget widget-gpu" aria-label="GPU monitor">
      <div class="widget__header">
        <span class="widget__eyebrow">GPU</span>
        <strong>${Math.round(state.gpuUsagePercent)}%</strong>
      </div>
      <div class="metric metric-large">
        <span>Load</span>
        <meter min="0" max="100" value="${state.gpuUsagePercent}"></meter>
      </div>
      <div class="metric-grid">
        <span>Temp <strong>${state.gpuTemperatureCelsius === null ? "--" : `${Math.round(state.gpuTemperatureCelsius)} C`}</strong></span>
        <span>VRAM <strong>${state.vramUsedGb.toFixed(1)} / ${state.vramTotalGb.toFixed(0)} GB</strong></span>
        <span>Power <strong>${formatReading(state.gpuPowerWatts, "W")}</strong></span>
        <span>Fans <strong>${formatReading(state.gpuFanRpm, "RPM")}</strong></span>
      </div>
      <div class="sparkline" aria-hidden="true"></div>
    </section>
  `;
}

function formatReading(value: number | null, unit: string): string {
  return value === null ? "--" : `${Math.round(value)} ${unit}`;
}
