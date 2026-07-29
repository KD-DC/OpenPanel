import type { MediaSummary } from "../../types";

export function renderMediaWidget(state: MediaSummary): string {
  const progress = state.durationSeconds > 0
    ? Math.min(100, (state.positionSeconds / state.durationSeconds) * 100)
    : 0;

  return `
    <section class="widget widget-media" aria-label="Media">
      <div class="artwork" aria-hidden="true">OP</div>
      <div class="media-copy">
        <span class="widget__eyebrow">${escapeHtml(state.source)}</span>
        <h1>${escapeHtml(state.title)}</h1>
        <p>${escapeHtml(state.artist || "No artist")}</p>
        <div class="progress" aria-label="Playback progress">
          <span style="inline-size:${progress}%"></span>
        </div>
        <div class="controls">
          <button type="button" data-command="previous" title="Previous">Prev</button>
          <button type="button" data-command="toggle" title="Play or pause">${state.isPlaying ? "Pause" : "Play"}</button>
          <button type="button" data-command="next" title="Next">Next</button>
        </div>
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
