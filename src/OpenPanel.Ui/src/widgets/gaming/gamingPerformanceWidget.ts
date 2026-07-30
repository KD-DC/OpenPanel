import type { GamingPerformanceSummary } from "../../types";

export function renderGamingPerformanceWidget(
  state: GamingPerformanceSummary
): string {
  const startDisabled = !state.collectorAvailable && !state.isActive;
  return `
    <section class="widget widget-gaming ${state.isActive ? "is-active" : ""}" aria-label="Gaming performance">
      <header class="gaming-header">
        <span class="widget__eyebrow"><i data-lucide="gamepad-2"></i>Gaming</span>
        <button
          type="button"
          data-command="gaming-toggle"
          aria-pressed="${state.isActive}"
          ${startDisabled ? "disabled" : ""}
          title="${state.isActive ? "Stop performance monitoring" : "Start performance monitoring"}">
          <i data-lucide="${state.isActive ? "square" : "play"}"></i>
          ${state.isActive ? "Stop" : "Start"}
        </button>
      </header>
      <div class="gaming-hero">
        <strong>${reading(state.fps, "FPS", 0)}</strong>
        <span>${escapeHtml(state.application || state.status)}</span>
      </div>
      <div class="gaming-metrics">
        ${metric("1% low", reading(state.onePercentLowFps, "FPS", 0))}
        ${metric("Frame time", reading(state.frameTimeMs, "ms", 1))}
        ${metric("GPU busy", reading(state.gpuBusyMs, "ms", 1))}
        ${metric("Stutters", state.isActive ? String(state.stutterCount) : "--")}
      </div>
      <footer class="gaming-status">
        <span class="${state.isActive ? "is-live" : ""}"></span>
        ${escapeHtml(state.status)}
      </footer>
    </section>
  `;
}

function metric(label: string, value: string): string {
  return `<span><small>${label}</small><strong>${value}</strong></span>`;
}

function reading(
  value: number | null,
  unit: string,
  decimals: number
): string {
  return value === null ? "--" : `${value.toFixed(decimals)} ${unit}`;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
