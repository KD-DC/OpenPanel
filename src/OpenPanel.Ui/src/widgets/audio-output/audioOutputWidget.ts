import type { AudioSummary } from "../../types";

export function renderAudioOutputWidget(state: AudioSummary): string {
  const outputs = state.outputs.length > 0
    ? state.outputs
    : [{ id: "none", name: "No devices yet", isDefault: true }];

  return `
    <section class="widget widget-audio" aria-label="Audio output">
      <div class="widget__header">
        <span class="widget__eyebrow">Audio Output</span>
        <strong>${state.volumePercent}%</strong>
      </div>
      <div class="output-list">
        ${outputs.map((output) => `
          <button type="button" class="${output.isDefault ? "is-active" : ""}" data-output-id="${output.id}">
            ${output.isDefault ? "[on] " : ""}${output.name}
          </button>
        `).join("")}
      </div>
      <div class="metric metric-large">
        <span>${state.isMuted ? "Muted" : state.currentOutput}</span>
        <meter min="0" max="100" value="${state.volumePercent}"></meter>
      </div>
      <div class="level-meter" aria-hidden="true"><span></span></div>
    </section>
  `;
}
