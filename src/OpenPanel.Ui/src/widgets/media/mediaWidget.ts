import type { MediaSummary } from "../../types";

export function renderMediaWidget(state: MediaSummary): string {
  const artwork = state.artworkDataUrl
    ? `<img src="${escapeHtml(state.artworkDataUrl)}" alt="">`
    : `<span>OP</span>`;

  return `
    <section class="widget widget-media" aria-label="Media">
      <div class="artwork" aria-hidden="true">${artwork}</div>
      <div class="media-copy">
        <span class="widget__eyebrow">${escapeHtml(state.source)}</span>
        <h1>${escapeHtml(state.title)}</h1>
        <p>${escapeHtml(state.artist || "No artist")}</p>
        <input
          class="media-progress"
          type="range"
          min="0"
          max="${Math.max(1, state.durationSeconds)}"
          value="${state.positionSeconds}"
          data-command="media-seek"
          aria-label="Playback position"
          ${state.canSeek ? "" : "disabled"}>
        <div class="timeline">
          <span>${formatTime(state.positionSeconds)}</span>
          <span>${formatTime(state.durationSeconds)}</span>
        </div>
        <div class="controls">
          <button type="button" data-command="media-previous" title="Previous" ${state.canGoPrevious ? "" : "disabled"}>Prev</button>
          <button type="button" data-command="media-toggle" title="Play or pause" ${state.canToggle ? "" : "disabled"}>${state.isPlaying ? "Pause" : "Play"}</button>
          <button type="button" data-command="media-next" title="Next" ${state.canGoNext ? "" : "disabled"}>Next</button>
        </div>
      </div>
    </section>
  `;
}

function formatTime(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds <= 0) {
    return "0:00";
  }

  const wholeSeconds = Math.floor(seconds);
  const minutes = Math.floor(wholeSeconds / 60);
  return `${minutes}:${String(wholeSeconds % 60).padStart(2, "0")}`;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
