import type {
  AdvancedTelemetrySummary,
  StorageDeviceSummary,
  StorageSummary,
  TelemetrySummary
} from "../../types";

export function renderMemoryWidget(state: AdvancedTelemetrySummary): string {
  const memory = state.memory;
  return `
    <section class="widget widget-advanced" aria-label="Memory monitor">
      ${header("Memory", `${Math.round(memory.loadPercent)}%`, "memory-stick")}
      ${usageMeter("Physical RAM", memory.loadPercent, `${formatGb(memory.usedGb)} / ${formatGb(memory.totalGb)}`, "memory-stick")}
      ${metrics([
        ["database", "Used", formatGb(memory.usedGb)],
        ["memory-stick", "Available", formatGb(memory.availableGb)],
        ["microchip", "Installed", formatGb(memory.totalGb)],
        ["activity", "Commit", `${formatGb(memory.virtualUsedGb)} / ${formatGb(memory.virtualTotalGb)}`]
      ])}
    </section>
  `;
}

export function renderCpuPowerWidget(
  state: AdvancedTelemetrySummary,
  system: TelemetrySummary
): string {
  return `
    <section class="widget widget-advanced" aria-label="CPU performance monitor">
      ${header("CPU Performance", `${Math.round(system.cpuUsagePercent)}%`, "cpu")}
      ${meter("Processor load", system.cpuUsagePercent, "gauge")}
      ${metrics([
        ["activity", "Average clock", formatClock(state.cpuAverageClockMhz)],
        ["zap", "Package power", formatReading(state.cpuPackagePowerWatts, "W")],
        ["thermometer", "Package temp", formatTemperature(system.cpuTemperatureCelsius)],
        ["gauge", "Sensor state", sensorCount([
          state.cpuAverageClockMhz,
          state.cpuPackagePowerWatts,
          system.cpuTemperatureCelsius
        ])]
      ])}
      ${availability([
        state.cpuAverageClockMhz,
        state.cpuPackagePowerWatts,
        system.cpuTemperatureCelsius
      ])}
    </section>
  `;
}

export function renderGpuPowerWidget(state: AdvancedTelemetrySummary): string {
  return `
    <section class="widget widget-advanced" aria-label="GPU performance monitor">
      ${header("GPU Performance", formatClock(state.gpuCoreClockMhz), "microchip")}
      ${metrics([
        ["activity", "Core clock", formatClock(state.gpuCoreClockMhz)],
        ["database", "Memory clock", formatClock(state.gpuMemoryClockMhz)],
        ["fan", "Fan control", formatReading(state.gpuFanPercent, "%")],
        ["gauge", "Sensor state", sensorCount([state.gpuCoreClockMhz, state.gpuMemoryClockMhz, state.gpuFanPercent])]
      ])}
      ${availability([state.gpuCoreClockMhz, state.gpuMemoryClockMhz, state.gpuFanPercent])}
    </section>
  `;
}

export function renderGpuThermalsWidget(state: AdvancedTelemetrySummary): string {
  return `
    <section class="widget widget-advanced" aria-label="GPU thermal monitor">
      ${header("GPU Thermals", formatTemperature(state.gpuHotSpotTemperatureCelsius), "thermometer")}
      <div class="thermal-stack">
        ${thermalRow("Hot spot", state.gpuHotSpotTemperatureCelsius)}
        ${thermalRow("Memory junction", state.gpuMemoryTemperatureCelsius)}
      </div>
      ${availability([state.gpuHotSpotTemperatureCelsius, state.gpuMemoryTemperatureCelsius])}
    </section>
  `;
}

export function renderStorageWidget(state: StorageSummary): string {
  const visibleDevices = state.devices.slice(0, 2);
  return `
    <section class="widget widget-advanced widget-storage" aria-label="Storage monitor">
      ${header("Storage", `${state.devices.length} drive${state.devices.length === 1 ? "" : "s"}`, "hard-drive")}
      ${visibleDevices.length === 0
        ? `<div class="empty-state"><i data-lucide="hard-drive"></i><span>No supported storage sensors</span></div>`
        : `<div class="storage-list">${visibleDevices.map(storageDevice).join("")}</div>`}
      ${state.devices.length > visibleDevices.length
        ? `<p class="sensor-availability">+${state.devices.length - visibleDevices.length} more detected</p>`
        : ""}
    </section>
  `;
}

function storageDevice(device: StorageDeviceSummary): string {
  const used = device.usedPercent ?? 0;
  const name = escapeHtml(device.name);
  return `
    <div class="storage-device">
      <div class="storage-device__header">
        <strong title="${name}">${name}</strong>
        <span>${formatTemperature(device.temperatureCelsius)}</span>
      </div>
      ${usageMeter("Used", used, device.usedPercent === null ? "--" : `${Math.round(device.usedPercent)}%`, "database", device.usedPercent !== null)}
      <div class="storage-device__stats">
        <span><i data-lucide="activity"></i>${formatReading(device.activityPercent, "%")}</span>
        <span><i data-lucide="download"></i>${formatRate(device.readMegabytesPerSecond)}</span>
        <span><i data-lucide="upload"></i>${formatRate(device.writeMegabytesPerSecond)}</span>
      </div>
    </div>
  `;
}

function header(label: string, value: string, icon: string): string {
  return `<div class="widget__header"><span class="widget__eyebrow"><i data-lucide="${icon}"></i>${label}</span><strong>${value}</strong></div>`;
}

function meter(label: string, value: number, icon: string): string {
  return `<div class="metric metric-large"><span class="metric-label"><i data-lucide="${icon}"></i>${label}</span><meter min="0" max="100" value="${value}"></meter></div>`;
}

function usageMeter(
  label: string,
  percent: number,
  value: string,
  icon: string,
  available = true
): string {
  const bounded = Math.max(0, Math.min(100, percent));
  return `
    <div class="usage-meter">
      <div class="usage-meter__label">
        <span class="metric-label"><i data-lucide="${icon}"></i>${label}</span>
        <strong>${value}</strong>
      </div>
      <div class="usage-meter__track${available ? "" : " is-unavailable"}" role="meter" aria-label="${label}" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${Math.round(bounded)}">
        <span style="width: ${bounded}%"></span>
      </div>
    </div>
  `;
}

function metrics(items: Array<[string, string, string]>): string {
  return `<div class="metric-grid">${items.map(([icon, label, value]) =>
    `<span><span class="metric-label"><i data-lucide="${icon}"></i>${label}</span><strong>${value}</strong></span>`).join("")}</div>`;
}

function thermalRow(label: string, value: number | null): string {
  return `<div><span>${label}</span><strong>${formatTemperature(value)}</strong></div>`;
}

function availability(values: Array<number | null>): string {
  return `<p class="sensor-availability">${sensorCount(values)} live sensor${values.filter(isPresent).length === 1 ? "" : "s"}</p>`;
}

function sensorCount(values: Array<number | null>): string {
  return `${values.filter(isPresent).length} / ${values.length}`;
}

function isPresent(value: number | null): value is number {
  return value !== null;
}

function formatGb(value: number): string {
  return `${value.toFixed(1)} GB`;
}

function formatClock(value: number | null): string {
  return value === null ? "--" : `${(value / 1000).toFixed(2)} GHz`;
}

function formatReading(value: number | null, unit: string): string {
  return value === null ? "--" : `${Math.round(value)} ${unit}`;
}

function formatTemperature(value: number | null): string {
  return value === null ? "--" : `${Math.round(value)} C`;
}

function formatRate(value: number | null): string {
  return value === null ? "--" : `${value.toFixed(value < 10 ? 1 : 0)} MB/s`;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("\"", "&quot;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}
