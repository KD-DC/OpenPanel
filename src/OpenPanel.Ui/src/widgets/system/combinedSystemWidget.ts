import type { GpuSummary, TelemetrySummary } from "../../types";

export function renderCombinedSystemWidget(
  system: TelemetrySummary,
  gpu: GpuSummary
): string {
  const memoryPercent = percentage(system.memoryUsedGb, system.memoryTotalGb);
  const vramPercent = percentage(gpu.vramUsedGb, gpu.vramTotalGb);
  return `
    <section class="widget widget-hardware" aria-label="System and graphics monitor">
      <span class="widget__eyebrow"><i data-lucide="microchip"></i>Hardware</span>
      <div class="hardware-loads">
        ${load("CPU", system.cpuUsagePercent, system.cpuTemperatureCelsius)}
        ${load("GPU", gpu.gpuUsagePercent, gpu.gpuTemperatureCelsius)}
      </div>
      ${usage("RAM", memoryPercent, `${system.memoryUsedGb.toFixed(1)} / ${system.memoryTotalGb.toFixed(0)} GB`)}
      ${usage("VRAM", vramPercent, `${gpu.vramUsedGb.toFixed(1)} / ${gpu.vramTotalGb.toFixed(0)} GB`)}
      <div class="hardware-pane__metrics hardware-pane__gpu-metrics">
        ${metric("GPU power", reading(gpu.gpuPowerWatts, "W"), "zap")}
        ${metric("GPU fans", reading(gpu.gpuFanRpm, "RPM"), "fan")}
      </div>
      <div class="hardware-pane__network">
        <span class="hardware-pane__section-title">
          <i data-lucide="network"></i>Network
        </span>
        <div class="hardware-pane__metrics">
          ${metric("Download", networkRate(system.networkDownloadMbps), "download")}
          ${metric("Upload", networkRate(system.networkUploadMbps), "upload")}
        </div>
      </div>
    </section>
  `;
}

function load(label: string, percent: number, temperatureValue: number | null): string {
  return `
    <div class="hardware-load">
      <span>${label}<small>${temperature(temperatureValue)}</small></span>
      <strong>${Math.round(percent)}%</strong>
      <div role="meter" aria-label="${label} load" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${Math.round(percent)}">
        <span style="inline-size:${Math.max(0, Math.min(100, percent))}%"></span>
      </div>
    </div>
  `;
}

function usage(label: string, percent: number, value: string): string {
  const clamped = Math.max(0, Math.min(100, percent));
  return `
    <div class="hardware-pane__usage">
      <span>${label}<strong>${value}</strong></span>
      <div role="meter" aria-label="${label}" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${Math.round(clamped)}">
        <span style="inline-size:${clamped}%"></span>
      </div>
    </div>
  `;
}

function metric(label: string, value: string, icon: string): string {
  return `
    <span>
      <small><i data-lucide="${icon}"></i>${label}</small>
      <strong>${value}</strong>
    </span>
  `;
}

function percentage(used: number, total: number): number {
  return total > 0 ? Math.max(0, Math.min(100, used / total * 100)) : 0;
}

function temperature(value: number | null): string {
  return value === null ? "--" : `${Math.round(value)} C`;
}

function reading(value: number | null, unit: string): string {
  return value === null ? "--" : `${Math.round(value)} ${unit}`;
}

function networkRate(megabitsPerSecond: number): string {
  if (megabitsPerSecond >= 1000) {
    return `${(megabitsPerSecond / 1000).toFixed(1)} Gbps`;
  }

  if (megabitsPerSecond >= 100) {
    return `${Math.round(megabitsPerSecond)} Mbps`;
  }

  if (megabitsPerSecond >= 1) {
    return `${megabitsPerSecond.toFixed(1)} Mbps`;
  }

  if (megabitsPerSecond > 0) {
    return `${Math.round(megabitsPerSecond * 1000)} Kbps`;
  }

  return "0 Mbps";
}
