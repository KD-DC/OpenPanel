import type { MediaSummary } from "../../types";

export function renderMediaWidget(
  state: MediaSummary,
  isCompact = false
): string {
  if (isCompact) {
    return renderCompactMediaWidget(state);
  }

  const artwork = state.artworkDataUrl
    ? `<img src="${escapeHtml(state.artworkDataUrl)}" alt="">`
    : `<span>OP</span>`;
  const metadata = renderMetadata(state);
  const playbackDetails = renderPlaybackDetails(state);

  return `
    <section class="widget widget-media" aria-label="Media">
      ${mediaSizeButton(false)}
      <div class="artwork" aria-hidden="true">${artwork}</div>
      <div class="media-copy">
        <span class="widget__eyebrow">${escapeHtml(state.source)}</span>
        <h1>${escapeHtml(state.title)}</h1>
        <p class="media-artist">${escapeHtml(state.artist || state.albumArtist || "No artist")}</p>
        ${metadata}
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
        ${playbackDetails}
        <div class="controls">
          <button type="button" data-command="media-previous" title="Previous" ${state.canGoPrevious ? "" : "disabled"}>Prev</button>
          <button type="button" data-command="media-toggle" title="Play or pause" ${state.canToggle ? "" : "disabled"}>${state.isPlaying ? "Pause" : "Play"}</button>
          <button type="button" data-command="media-next" title="Next" ${state.canGoNext ? "" : "disabled"}>Next</button>
          ${state.canShuffle
            ? `<button class="media-shuffle${state.isShuffleActive ? " is-active" : ""}" type="button" data-command="media-shuffle" title="${state.isShuffleActive ? "Turn shuffle off" : "Turn shuffle on"}" aria-label="${state.isShuffleActive ? "Turn shuffle off" : "Turn shuffle on"}" aria-pressed="${state.isShuffleActive === true}"><i data-lucide="shuffle"></i></button>`
            : ""}
        </div>
      </div>
    </section>
  `;
}

function renderCompactMediaWidget(state: MediaSummary): string {
  const artwork = state.artworkDataUrl
    ? `<img src="${escapeHtml(state.artworkDataUrl)}" alt="">`
    : `<span>OP</span>`;
  return `
    <section class="widget widget-media widget-media--compact" aria-label="Media">
      ${mediaSizeButton(true)}
      <div class="media-compact__art" aria-hidden="true">${artwork}</div>
      <span class="widget__eyebrow">${escapeHtml(state.source)}</span>
      <h1>${escapeHtml(state.title)}</h1>
      <p>${escapeHtml(state.artist || state.albumArtist || "No artist")}</p>
      <div class="media-compact__controls">
        <button type="button" data-command="media-previous" title="Previous" aria-label="Previous" ${state.canGoPrevious ? "" : "disabled"}><i data-lucide="skip-back"></i></button>
        <button type="button" data-command="media-toggle" title="${state.isPlaying ? "Pause" : "Play"}" aria-label="${state.isPlaying ? "Pause" : "Play"}" ${state.canToggle ? "" : "disabled"}><i data-lucide="${state.isPlaying ? "pause" : "play"}"></i></button>
        <button type="button" data-command="media-next" title="Next" aria-label="Next" ${state.canGoNext ? "" : "disabled"}><i data-lucide="skip-forward"></i></button>
      </div>
    </section>
  `;
}

function mediaSizeButton(isCompact: boolean): string {
  return `
    <button
      class="media-size-toggle"
      type="button"
      data-command="media-size"
      title="${isCompact ? "Expand media" : "Collapse media"}"
      aria-label="${isCompact ? "Expand media" : "Collapse media"}"
      aria-pressed="${!isCompact}">
      <i data-lucide="${isCompact ? "maximize-2" : "minimize-2"}"></i>
    </button>
  `;
}

function renderMetadata(state: MediaSummary): string {
  const values: string[] = [];
  if (state.album) {
    values.push(state.album);
  }
  if (state.trackNumber > 0) {
    const trackCount = state.albumTrackCount > 0 ? `/${state.albumTrackCount}` : "";
    values.push(`Track ${state.trackNumber}${trackCount}`);
  }
  if (state.subtitle) {
    values.push(state.subtitle);
  }
  if (state.genres.length > 0) {
    values.push(state.genres.slice(0, 2).join(", "));
  }

  return values.length > 0
    ? `<p class="media-metadata">${values.map(escapeHtml).join("<span aria-hidden=\"true\">&middot;</span>")}</p>`
    : "";
}

function renderPlaybackDetails(state: MediaSummary): string {
  const details: string[] = [];
  if (state.playbackStatus && state.playbackStatus !== "Closed") {
    details.push(state.playbackStatus);
  }
  if (state.repeatMode && state.repeatMode !== "None") {
    details.push(formatRepeatMode(state.repeatMode));
  }
  if (state.playbackRate !== null && Math.abs(state.playbackRate - 1) > 0.01) {
    details.push(`${state.playbackRate.toFixed(2).replace(/0+$/, "").replace(/\.$/, "")}x`);
  }
  if (state.playbackType && state.playbackType !== "Unknown") {
    details.push(state.playbackType);
  }

  return details.length > 0
    ? `<p class="media-status" aria-label="Playback details">${details
        .map(escapeHtml)
        .join("<span aria-hidden=\"true\">&middot;</span>")}</p>`
    : "";
}

function formatRepeatMode(value: string): string {
  switch (value) {
    case "Track":
      return "Repeat one";
    case "List":
      return "Repeat all";
    default:
      return `Repeat ${value.toLowerCase()}`;
  }
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
