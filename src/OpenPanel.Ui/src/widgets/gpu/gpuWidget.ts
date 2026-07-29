import type { GpuSummary } from "../../types";

export function renderGpuWidget(state: GpuSummary): string {
  const vramPercent = percentage(state.vramUsedGb, state.vramTotalGb);
  return `
    <section class="widget widget-gpu" aria-label="GPU monitor">
      <div class="widget__header">
        <span class="widget__eyebrow"><i data-lucide="microchip"></i>GPU</span>
        <strong>${Math.round(state.gpuUsagePercent)}%</strong>
      </div>
      <div class="metric metric-large">
        <span class="metric-label"><i data-lucide="gauge"></i>GPU load</span>
        <meter min="0" max="100" value="${state.gpuUsagePercent}"></meter>
      </div>
      <div class="usage-meter">
        <div class="usage-meter__label">
          <span class="metric-label"><i data-lucide="database"></i>VRAM</span>
          <strong>${state.vramUsedGb.toFixed(1)} / ${state.vramTotalGb.toFixed(0)} GB · ${Math.round(vramPercent)}%</strong>
        </div>
        <div class="usage-meter__track" role="meter" aria-label="VRAM usage" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${Math.round(vramPercent)}">
          <span style="width: ${vramPercent}%"></span>
        </div>
      </div>
      <div class="metric-grid">
        ${metric("thermometer", "Temp", state.gpuTemperatureCelsius === null ? "--" : `${Math.round(state.gpuTemperatureCelsius)} C`)}
        ${metric("zap", "Power", formatReading(state.gpuPowerWatts, "W"))}
        ${metric("fan", "Fans", formatReading(state.gpuFanRpm, "RPM"))}
      </div>
    </section>
  `;
}

function metric(icon: string, label: string, value: string): string {
  return `<span><span class="metric-label"><i data-lucide="${icon}"></i>${label}</span><strong>${value}</strong></span>`;
}

function percentage(used: number, total: number): number {
  return total > 0 ? Math.max(0, Math.min(100, used / total * 100)) : 0;
}

function formatReading(value: number | null, unit: string): string {
  return value === null ? "--" : `${Math.round(value)} ${unit}`;
}
