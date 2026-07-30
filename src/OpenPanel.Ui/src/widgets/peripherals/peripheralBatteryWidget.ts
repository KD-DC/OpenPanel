import type {
  PeripheralBatteryDeviceSummary,
  PeripheralBatterySummary
} from "../../types";

export function renderPeripheralBatteryWidget(
  state: PeripheralBatterySummary
): string {
  const devices = state.devices.filter(
    (device) =>
      device.batteryPercent !== null ||
      device.batteryState !== null
  );
  return `
    <section class="widget widget-peripherals" aria-label="Peripheral batteries">
      <span class="widget__eyebrow"><i data-lucide="battery-charging"></i>Batteries</span>
      <div class="peripheral-list">
        ${devices.length > 0
          ? devices.slice(0, 4).map(renderDevice).join("")
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
  const batteryState = device.batteryState;
  const icon = categoryIcon(device.category);
  const status = !device.isConnected
    ? "Disconnected"
    : percent === null && batteryState !== null
      ? `Battery status${device.isCharging ? " · charging" : ""}`
      : percent === null
        ? "Battery unavailable"
      : `${Math.round(percent)}%${device.isCharging ? " charging" : ""}`;
  const value = percent === null
    ? batteryState ?? "--"
    : `${Math.round(percent)}%`;
  return `
    <article class="peripheral-device ${device.isConnected ? "" : "is-disconnected"}">
      <i data-lucide="${icon}"></i>
      <div>
        <span title="${escapeHtml(device.name)}">${escapeHtml(device.name)}</span>
        <small>${status}</small>
        ${percent === null
          ? ""
          : `<div role="meter" aria-label="${escapeHtml(device.name)} battery"
              aria-valuemin="0" aria-valuemax="100"
              aria-valuenow="${Math.round(percent)}">
              <span style="inline-size:${Math.max(0, Math.min(100, percent))}%"></span>
            </div>`}
      </div>
      <strong class="${percent === null ? "is-text-state" : ""}">${escapeHtml(value)}</strong>
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
