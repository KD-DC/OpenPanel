import type {
  GpuSummary,
  NetworkQualitySummary,
  ProcessUsageApplicationSummary,
  ProcessUsageSummary,
  TelemetrySummary
} from "../../types";

export function renderCombinedSystemWidget(
  system: TelemetrySummary,
  gpu: GpuSummary,
  network: NetworkQualitySummary,
  processes: ProcessUsageSummary
): string {
  const memoryPercent = percentage(system.memoryUsedGb, system.memoryTotalGb);
  const vramPercent = percentage(gpu.vramUsedGb, gpu.vramTotalGb);
  return `
    <section class="widget widget-hardware" aria-label="System and graphics monitor">
      <header class="hardware-pane__header">
        <span class="widget__eyebrow"><i data-lucide="microchip"></i>Hardware</span>
        <button
          type="button"
          data-command="hardware-expand"
          aria-expanded="false"
          title="Open application usage"
          aria-label="Open application usage">
          <i data-lucide="maximize-2"></i>
        </button>
      </header>
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
        <button
          type="button"
          class="hardware-pane__section-title hardware-pane__network-button"
          data-command="network-expand"
          aria-expanded="false"
          title="Open network diagnostics">
          <i data-lucide="network"></i>Network
          <i data-lucide="maximize-2"></i>
        </button>
        <div class="hardware-pane__metrics">
          ${metric("Download", networkRate(system.networkDownloadMbps), "download")}
          ${metric("Upload", networkRate(system.networkUploadMbps), "upload")}
        </div>
      </div>
    </section>
    ${renderProcessUsage(processes)}
    ${renderNetworkQuality(system, network)}
  `;
}

function renderProcessUsage(processes: ProcessUsageSummary): string {
  return `
    <section class="hardware-processes" aria-label="Application CPU and memory usage">
      <header class="hardware-processes__header">
        <div>
          <span><i data-lucide="microchip"></i>Hardware activity</span>
          <strong>Application usage</strong>
        </div>
        <button
          type="button"
          data-command="hardware-expand"
          aria-expanded="true"
          title="Close application usage"
          aria-label="Close application usage">
          <i data-lucide="minimize-2"></i>
        </button>
      </header>
      <div class="hardware-processes__rankings">
        ${processRanking(
          "Top CPU",
          "cpu",
          processes.topCpu,
          application => `${application.cpuPercent.toFixed(1)}%`,
          application => application.cpuPercent
        )}
        ${processRanking(
          "Top memory",
          "memory-stick",
          processes.topMemory,
          application => memorySize(application.memoryMegabytes),
          (application, applications) =>
            applications[0]?.memoryMegabytes
              ? application.memoryMegabytes / applications[0].memoryMegabytes * 100
              : 0
        )}
      </div>
      <footer>
        <i data-lucide="info"></i>
        ${escapeHtml(processes.status)}. Samples every 2 seconds and stops when this view closes.
      </footer>
    </section>
  `;
}

function processRanking(
  label: string,
  icon: string,
  applications: ProcessUsageApplicationSummary[],
  formatValue: (application: ProcessUsageApplicationSummary) => string,
  barPercent: (
    application: ProcessUsageApplicationSummary,
    applications: ProcessUsageApplicationSummary[]
  ) => number
): string {
  const rows = applications.length > 0
    ? applications.map(application => {
        const percent = Math.max(0, Math.min(100, barPercent(application, applications)));
        return `
          <li>
            <span title="${escapeHtml(application.name)}">${escapeHtml(application.name)}</span>
            <strong>${formatValue(application)}</strong>
            <div aria-hidden="true"><span style="inline-size:${percent}%"></span></div>
          </li>
        `;
      }).join("")
    : `<li class="hardware-processes__empty">Collecting the first sample...</li>`;

  return `
    <section>
      <header><i data-lucide="${icon}"></i><span>${label}</span></header>
      <ol>${rows}</ol>
    </section>
  `;
}

function renderNetworkQuality(
  system: TelemetrySummary,
  network: NetworkQualitySummary
): string {
  return `
    <section class="network-quality" aria-label="Network quality">
      <header class="network-quality__header">
        <div>
          <span><i data-lucide="radio"></i>Network quality</span>
          <strong>${escapeHtml(network.status)}</strong>
        </div>
        <button
          type="button"
          data-command="network-expand"
          aria-expanded="true"
          title="Close network diagnostics"
          aria-label="Close network diagnostics">
          <i data-lucide="minimize-2"></i>
        </button>
      </header>
      <div class="network-quality__primary">
        ${qualityMetric("Latency", readingDecimal(network.latencyMs, "ms"), "gauge")}
        ${qualityMetric("Jitter", readingDecimal(network.jitterMs, "ms"), "activity")}
        ${qualityMetric("Packet loss", readingDecimal(network.packetLossPercent, "%"), "circle-alert")}
        ${qualityMetric("Link speed", network.linkSpeedMbps === null ? "--" : networkRate(network.linkSpeedMbps), "network")}
      </div>
      ${renderApplicationTraffic(system, network)}
      <div class="network-quality__details">
        ${detail("Interface", network.interfaceName)}
        ${detail("Connection", network.connectionType)}
        ${detail("Local address", network.localAddress)}
        ${detail("Probe target", network.target)}
      </div>
      <footer>
        <i data-lucide="info"></i>
        ${escapeHtml(network.applicationTraffic.status)}. Diagnostics stop when this view closes.
      </footer>
    </section>
  `;
}

function renderApplicationTraffic(
  system: TelemetrySummary,
  network: NetworkQualitySummary
): string {
  const apps = network.applicationTraffic.applications;
  if (network.applicationTraffic.requiresPermission) {
    return `
      <div class="network-apps network-apps--permission">
        <div>
          <i data-lucide="shield-alert"></i>
          <span>
            <strong>Application tracking needs Windows permission</strong>
            <small>OpenPanel remains a standard, non-admin application.</small>
          </span>
        </div>
        <button type="button" data-command="network-permission">
          <i data-lucide="shield-check"></i>
          Enable app tracking
        </button>
      </div>
    `;
  }

  const downloads = [...apps]
    .filter(app => app.downloadMbps > 0)
    .sort((left, right) => right.downloadMbps - left.downloadMbps)
    .slice(0, 3);
  const uploads = [...apps]
    .filter(app => app.uploadMbps > 0)
    .sort((left, right) => right.uploadMbps - left.uploadMbps)
    .slice(0, 3);

  return `
    <div class="network-apps" aria-label="Application network traffic">
      ${applicationTrafficColumn(
        "Download",
        system.networkDownloadMbps,
        downloads.map(app => ({ name: app.name, rate: app.downloadMbps })),
        "download"
      )}
      ${applicationTrafficColumn(
        "Upload",
        system.networkUploadMbps,
        uploads.map(app => ({ name: app.name, rate: app.uploadMbps })),
        "upload"
      )}
    </div>
  `;
}

function applicationTrafficColumn(
  label: string,
  totalMbps: number,
  applications: Array<{ name: string; rate: number }>,
  icon: string
): string {
  const rows = applications.length > 0
    ? applications.map(application => `
        <li>
          <span title="${escapeHtml(application.name)}">${escapeHtml(application.name)}</span>
          <strong>${networkRate(application.rate)}</strong>
        </li>
      `).join("")
    : `<li class="network-apps__empty">No active application traffic</li>`;

  return `
    <section>
      <header>
        <span><i data-lucide="${icon}"></i>${label}</span>
        <strong>${networkRate(totalMbps)}</strong>
      </header>
      <ul>${rows}</ul>
    </section>
  `;
}

function qualityMetric(label: string, value: string, icon: string): string {
  return `<div><i data-lucide="${icon}"></i><span>${label}</span><strong>${value}</strong></div>`;
}

function detail(label: string, value: string): string {
  return `<span><small>${label}</small><strong>${escapeHtml(value)}</strong></span>`;
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

function readingDecimal(value: number | null, unit: string): string {
  return value === null ? "--" : `${value.toFixed(1)} ${unit}`;
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

function memorySize(megabytes: number): string {
  return megabytes >= 1024
    ? `${(megabytes / 1024).toFixed(1)} GB`
    : `${Math.round(megabytes)} MB`;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
