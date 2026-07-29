import type { AudioSummary } from "../../types";

export function renderAudioOutputWidget(state: AudioSummary): string {
  const outputs = state.outputs.slice(0, 6);

  return `
    <section class="widget widget-audio" aria-label="Audio output">
      <div class="widget__header">
        <span class="widget__eyebrow">Audio Output</span>
        <strong>${state.volumePercent}%</strong>
      </div>
      <div class="output-list">
        ${outputs.length > 0
          ? outputs.map((output) => `
              <button
                type="button"
                class="${output.isDefault ? "is-active" : ""}"
                data-output-id="${escapeHtml(output.id)}"
                title="${escapeHtml(output.name)}">
                <span class="output-status" aria-hidden="true">${output.isDefault ? "ON" : ""}</span>
                <span>${escapeHtml(output.name)}</span>
              </button>
            `).join("")
          : `<span class="empty-state">No active outputs</span>`}
      </div>
      <div class="audio-controls">
        <button type="button" data-command="audio-mute">${state.isMuted ? "Unmute" : "Mute"}</button>
        <label>
          <span>${state.isMuted ? "Muted" : escapeHtml(state.currentOutput)}</span>
          <input
            type="range"
            min="0"
            max="100"
            value="${state.volumePercent}"
            data-command="audio-volume"
            aria-label="Global volume">
        </label>
      </div>
      <label class="communications-toggle">
        <input type="checkbox" data-setting="communications" ${state.setCommunicationsDevice ? "checked" : ""}>
        <span>Also set communications output</span>
      </label>
      <div class="level-meter" aria-label="Audio activity">
        <span style="inline-size:${state.peakLevelPercent}%"></span>
      </div>
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
