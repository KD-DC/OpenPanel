import type { TelemetrySummary } from "../../types";

export function renderSystemWidget(state: TelemetrySummary): string {
  return `
    <section class="widget widget-system" aria-label="System monitor">
      <div class="widget__header">
        <span class="widget__eyebrow">System</span>
        <strong>${formatPercent(state.cpuUsagePercent)}</strong>
      </div>
      <div class="metric metric-large">
        <span>CPU</span>
        <meter min="0" max="100" value="${state.cpuUsagePercent}"></meter>
      </div>
      <div class="metric-grid">
        <span>Temp <strong>${formatTemp(state.cpuTemperatureCelsius)}</strong></span>
        <span>RAM <strong>${state.memoryUsedGb.toFixed(1)} / ${state.memoryTotalGb.toFixed(0)} GB</strong></span>
        <span>Up <strong>${state.networkUploadMbps.toFixed(1)} Mbps</strong></span>
        <span>Down <strong>${state.networkDownloadMbps.toFixed(1)} Mbps</strong></span>
      </div>
      <div class="sparkline" aria-hidden="true"></div>
    </section>
  `;
}

function formatPercent(value: number): string {
  return `${Math.round(value)}%`;
}

function formatTemp(value: number | null): string {
  return value === null ? "--" : `${Math.round(value)} C`;
}
