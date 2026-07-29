import type { AdvancedTelemetrySummary, TelemetrySummary } from "../../types";

export function renderMemoryWidget(state: AdvancedTelemetrySummary): string {
  const memory = state.memory;
  return `
    <section class="widget widget-advanced" aria-label="Memory monitor">
      ${header("Memory", `${Math.round(memory.loadPercent)}%`)}
      ${meter("Physical RAM", memory.loadPercent)}
      ${metrics([
        ["Used", formatGb(memory.usedGb)],
        ["Available", formatGb(memory.availableGb)],
        ["Installed", formatGb(memory.totalGb)],
        ["Commit", `${formatGb(memory.virtualUsedGb)} / ${formatGb(memory.virtualTotalGb)}`]
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
      ${header("CPU Performance", `${Math.round(system.cpuUsagePercent)}%`)}
      ${meter("Processor load", system.cpuUsagePercent)}
      ${metrics([
        ["Average clock", formatClock(state.cpuAverageClockMhz)],
        ["Package power", formatReading(state.cpuPackagePowerWatts, "W")],
        ["Package temp", formatTemperature(system.cpuTemperatureCelsius)],
        ["Sensor state", sensorCount([
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
      ${header("GPU Performance", formatClock(state.gpuCoreClockMhz))}
      ${metrics([
        ["Core clock", formatClock(state.gpuCoreClockMhz)],
        ["Memory clock", formatClock(state.gpuMemoryClockMhz)],
        ["Fan control", formatReading(state.gpuFanPercent, "%")],
        ["Sensor state", sensorCount([state.gpuCoreClockMhz, state.gpuMemoryClockMhz, state.gpuFanPercent])]
      ])}
      ${availability([state.gpuCoreClockMhz, state.gpuMemoryClockMhz, state.gpuFanPercent])}
    </section>
  `;
}

export function renderGpuThermalsWidget(state: AdvancedTelemetrySummary): string {
  return `
    <section class="widget widget-advanced" aria-label="GPU thermal monitor">
      ${header("GPU Thermals", formatTemperature(state.gpuHotSpotTemperatureCelsius))}
      <div class="thermal-stack">
        ${thermalRow("Hot spot", state.gpuHotSpotTemperatureCelsius)}
        ${thermalRow("Memory junction", state.gpuMemoryTemperatureCelsius)}
      </div>
      ${availability([state.gpuHotSpotTemperatureCelsius, state.gpuMemoryTemperatureCelsius])}
    </section>
  `;
}

function header(label: string, value: string): string {
  return `<div class="widget__header"><span class="widget__eyebrow">${label}</span><strong>${value}</strong></div>`;
}

function meter(label: string, value: number): string {
  return `<div class="metric metric-large"><span>${label}</span><meter min="0" max="100" value="${value}"></meter></div>`;
}

function metrics(items: Array<[string, string]>): string {
  return `<div class="metric-grid">${items.map(([label, value]) =>
    `<span>${label}<strong>${value}</strong></span>`).join("")}</div>`;
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
