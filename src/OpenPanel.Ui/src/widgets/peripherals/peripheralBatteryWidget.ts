import type {
  PeripheralBatteryDeviceSummary,
  PeripheralBatterySummary
} from "../../types";

export function renderPeripheralBatteryWidget(
  state: PeripheralBatterySummary
): string {
  return `
    <section class="widget widget-peripherals" aria-label="Peripheral batteries">
      <span class="widget__eyebrow"><i data-lucide="battery-charging"></i>Batteries</span>
      <div class="peripheral-list">
        ${state.devices.length > 0
          ? state.devices.slice(0, 4).map(renderDevice).join("")
          : `<div class="peripheral-empty">
              <i data-lucide="battery-medium"></i>
              <strong>No battery data</strong>
              <span>Connected Bluetooth and Logitech devices appear here.</span>
            </div>`}
      </div>
      <footer class="peripheral-footer">
        <i data-lucide="refresh-cw"></i>Refreshes every 2 minutes
      </footer>
    </section>
  `;
}

function renderDevice(device: PeripheralBatteryDeviceSummary): string {
  const percent = device.batteryPercent;
  const icon = categoryIcon(device.category);
  const status = !device.isConnected
    ? "Disconnected"
    : percent === null
      ? "Battery unavailable"
      : `${Math.round(percent)}%${device.isCharging ? " charging" : ""}`;
  return `
    <article class="peripheral-device ${device.isConnected ? "" : "is-disconnected"}">
      <i data-lucide="${icon}"></i>
      <div>
        <span title="${escapeHtml(device.name)}">${escapeHtml(device.name)}</span>
        <small>${status}</small>
        <div role="meter" aria-label="${escapeHtml(device.name)} battery"
          aria-valuemin="0" aria-valuemax="100"
          ${percent === null ? "" : `aria-valuenow="${Math.round(percent)}"`}>
          <span style="inline-size:${percent === null ? 0 : Math.max(0, Math.min(100, percent))}%"></span>
        </div>
      </div>
      <strong>${percent === null ? "--" : `${Math.round(percent)}%`}</strong>
    </article>
  `;
}

function categoryIcon(category: string): string {
  if (category === "Mouse") return "mouse";
  if (category === "Keyboard") return "keyboard";
  if (category === "Headphones") return "headphones";
  if (category === "Controller") return "gamepad-2";
  return "bluetooth";
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
