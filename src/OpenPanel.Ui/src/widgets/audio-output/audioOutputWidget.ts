import type { AudioSummary } from "../../types";

export function renderAudioOutputWidget(state: AudioSummary): string {
  const outputs = state.outputs.slice(0, 6);

  return `
    <section class="widget widget-audio" aria-label="Audio output">
      <div class="widget__header">
        <span class="widget__eyebrow">Audio Output</span>
        <div class="audio-center__heading">
          <strong>${state.volumePercent}%</strong>
          ${renderExpandButton()}
        </div>
      </div>
      <div class="audio-center__body">
        <div class="audio-center__playback">
          <div class="output-list">
            ${outputs.length > 0
              ? outputs.map((output) => {
                  const status = outputStatus(output.isDefault, state.isMuted);
                  return `
                    <button
                      type="button"
                      class="${output.isDefault ? "is-active" : ""}"
                      data-output-id="${escapeHtml(output.id)}"
                      title="${escapeHtml(output.name)}">
                      <span class="output-status" aria-hidden="true">${status}</span>
                      <span>${escapeHtml(output.name)}</span>
                    </button>
                  `;
                }).join("")
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
        </div>
        ${renderAudioExpandedContent(state)}
      </div>
    </section>
  `;
}

export function renderExpandButton(): string {
  return `
    <button
      class="audio-center__expand"
      type="button"
      data-command="audio-expand"
      title="Expand audio controls"
      aria-label="Expand audio controls"
      aria-expanded="false">
      <i class="audio-center__expand-icon" data-lucide="maximize-2"></i>
      <i class="audio-center__collapse-icon" data-lucide="minimize-2"></i>
    </button>
  `;
}

export function renderAudioExpandedContent(state: AudioSummary): string {
  const inputs = state.inputs.slice(0, 4);
  const sessions = state.sessions.slice(0, 5);

  return `
    <div class="audio-center__expanded">
      <section class="audio-center__input" aria-label="Microphone controls">
        <header class="audio-center__section-head">
          <span><i data-lucide="mic"></i>Input</span>
          <strong>${state.inputVolumePercent}%</strong>
        </header>
        <div class="audio-center__input-list">
          ${inputs.length > 0
            ? inputs.map((input) => `
                <button
                  type="button"
                  class="${input.isDefault ? "is-active" : ""}"
                  data-input-id="${escapeHtml(input.id)}"
                  title="${escapeHtml(input.name)}">
                  <i data-lucide="mic"></i>
                  <span>${escapeHtml(input.name)}</span>
                </button>
              `).join("")
            : `<span class="audio-center__empty">No active inputs</span>`}
        </div>
        <div class="audio-center__input-controls">
          <button
            type="button"
            data-command="audio-input-mute"
            title="${state.isInputMuted ? "Unmute microphone" : "Mute microphone"}"
            aria-label="${state.isInputMuted ? "Unmute microphone" : "Mute microphone"}"
            aria-pressed="${state.isInputMuted}">
            <i data-lucide="${state.isInputMuted ? "mic-off" : "mic"}"></i>
          </button>
          <label>
            <span>${state.isInputMuted ? "Microphone muted" : escapeHtml(state.currentInput)}</span>
            <input
              type="range"
              min="0"
              max="100"
              value="${state.inputVolumePercent}"
              data-command="audio-input-volume"
              aria-label="Microphone volume">
          </label>
          <div class="audio-center__mic-meter" role="meter" aria-label="Microphone activity" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${state.inputPeakLevelPercent}">
            <span style="inline-size:${state.inputPeakLevelPercent}%"></span>
          </div>
        </div>
      </section>
      <section class="audio-center__sessions" aria-label="Application volumes">
        <header class="audio-center__section-head">
          <span><i data-lucide="audio-lines"></i>Applications</span>
          <small>${sessions.length} active</small>
        </header>
        <div class="audio-center__session-list">
          ${sessions.length > 0
            ? sessions.map((session) => `
                <div class="audio-center__session">
                  <span class="audio-center__session-name" title="${escapeHtml(session.name)}">
                    <span class="audio-center__session-activity" style="block-size:${Math.max(4, Math.min(30, session.peakLevelPercent * 0.3))}px"></span>
                    ${escapeHtml(session.name)}
                  </span>
                  <button
                    type="button"
                    data-command="audio-session-mute"
                    data-session-id="${escapeHtml(session.id)}"
                    title="${session.isMuted ? `Unmute ${escapeHtml(session.name)}` : `Mute ${escapeHtml(session.name)}`}"
                    aria-label="${session.isMuted ? `Unmute ${escapeHtml(session.name)}` : `Mute ${escapeHtml(session.name)}`}"
                    aria-pressed="${session.isMuted}">
                    <i data-lucide="${session.isMuted ? "volume-x" : "volume-2"}"></i>
                  </button>
                  <input
                    type="range"
                    min="0"
                    max="100"
                    value="${session.volumePercent}"
                    data-command="audio-session-volume"
                    data-session-id="${escapeHtml(session.id)}"
                    aria-label="${escapeHtml(session.name)} volume">
                  <strong>${session.volumePercent}%</strong>
                </div>
              `).join("")
            : `<span class="audio-center__empty">Play audio to show application controls</span>`}
        </div>
      </section>
    </div>
  `;
}

function outputStatus(isDefault: boolean, isMuted: boolean): string {
  if (!isDefault) {
    return "";
  }

  return isMuted ? "MUTE" : "ON";
}

export function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
