import type { TelemetrySummary } from "../../types";

export function renderSystemWidget(state: TelemetrySummary): string {
  const memoryPercent = percentage(state.memoryUsedGb, state.memoryTotalGb);
  return `
    <section class="widget widget-system" aria-label="System monitor">
      <div class="widget__header">
        <span class="widget__eyebrow"><i data-lucide="cpu"></i>System</span>
        <strong>${formatPercent(state.cpuUsagePercent)}</strong>
      </div>
      <div class="metric metric-large">
        <span class="metric-label"><i data-lucide="gauge"></i>CPU load</span>
        <meter min="0" max="100" value="${state.cpuUsagePercent}"></meter>
      </div>
      ${usageBar(
        "memory-stick",
        "RAM",
        memoryPercent,
        `${state.memoryUsedGb.toFixed(1)} / ${state.memoryTotalGb.toFixed(0)} GB`
      )}
      <div class="metric-grid">
        ${metric("thermometer", "Temp", formatTemp(state.cpuTemperatureCelsius))}
        ${metric("upload", "Up", `${state.networkUploadMbps.toFixed(1)} Mbps`)}
        ${metric("download", "Down", `${state.networkDownloadMbps.toFixed(1)} Mbps`)}
      </div>
    </section>
  `;
}

function metric(icon: string, label: string, value: string): string {
  return `<span><span class="metric-label"><i data-lucide="${icon}"></i>${label}</span><strong>${value}</strong></span>`;
}

function usageBar(
  icon: string,
  label: string,
  percent: number,
  value: string
): string {
  return `
    <div class="usage-meter">
      <div class="usage-meter__label">
        <span class="metric-label"><i data-lucide="${icon}"></i>${label}</span>
        <strong>${value} · ${Math.round(percent)}%</strong>
      </div>
      <div class="usage-meter__track" role="meter" aria-label="${label} usage" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${Math.round(percent)}">
        <span style="width: ${percent}%"></span>
      </div>
    </div>
  `;
}

function percentage(used: number, total: number): number {
  return total > 0 ? Math.max(0, Math.min(100, used / total * 100)) : 0;
}

function formatPercent(value: number): string {
  return `${Math.round(value)}%`;
}

function formatTemp(value: number | null): string {
  return value === null ? "--" : `${Math.round(value)} C`;
}
