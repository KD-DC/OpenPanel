import type {
  MotherboardSummary,
  NamedSensorSummary
} from "../../types";

export function renderMotherboardThermalsWidget(
  state: MotherboardSummary
): string {
  return renderSensorWidget(
    "Board Thermals",
    "Motherboard thermal monitor",
    state.temperatures,
    (value) => `${Math.round(value)} C`
  );
}

export function renderMotherboardCoolingWidget(
  state: MotherboardSummary
): string {
  return renderSensorWidget(
    "Board Cooling",
    "Motherboard cooling monitor",
    state.fans,
    (value) => `${Math.round(value)} RPM`
  );
}

export function renderMotherboardVoltagesWidget(
  state: MotherboardSummary
): string {
  return renderSensorWidget(
    "Board Voltages",
    "Motherboard voltage monitor",
    state.voltages,
    (value) => `${value.toFixed(2)} V`
  );
}

export function renderMotherboardPowerWidget(
  state: MotherboardSummary
): string {
  return renderSensorWidget(
    "Board Power",
    "Motherboard power monitor",
    state.power,
    (value) => `${value.toFixed(1)} W`
  );
}

function renderSensorWidget(
  label: string,
  ariaLabel: string,
  sensors: NamedSensorSummary[],
  format: (value: number) => string
): string {
  const visible = sensors.slice(0, 5);
  return `
    <section class="widget widget-advanced" aria-label="${ariaLabel}">
      <div class="widget__header">
        <span class="widget__eyebrow">${label}</span>
        <strong>${visible.length}</strong>
      </div>
      ${visible.length > 0
        ? `<dl class="sensor-list">${visible.map((sensor) => `
            <div>
              <dt title="${escapeHtml(sensor.name)}">${escapeHtml(sensor.name)}</dt>
              <dd>${format(sensor.value)}</dd>
            </div>
          `).join("")}</dl>`
        : `<span class="empty-state">Not exposed by LibreHardwareMonitor</span>`}
      <p class="sensor-availability">${visible.length} live sensor${visible.length === 1 ? "" : "s"}</p>
    </section>
  `;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
