import type {
  AudioSummary,
  GpuSummary,
  MediaSummary,
  TelemetrySummary
} from "../../types";
import {
  renderAudioExpandedContent,
  renderExpandButton
} from "../audio-output/audioOutputWidget";

export function renderOledSystemWidget(state: TelemetrySummary): string {
  const memoryPercent = percentage(state.memoryUsedGb, state.memoryTotalGb);
  return `
    <section class="oled-zone oled-metric" style="--oled-accent:var(--oled-green)" aria-label="System monitor">
      <span class="oled-title"><i data-lucide="cpu"></i>System</span>
      <div class="oled-primary">${Math.round(state.cpuUsagePercent)}<small>%</small></div>
      <span class="oled-primary-label">CPU load</span>
      ${reading(
        "Current utilization",
        `${Math.round(state.cpuUsagePercent)} of 100`,
        state.cpuUsagePercent
      )}
      ${reading(
        "RAM",
        `${state.memoryUsedGb.toFixed(1)} / ${state.memoryTotalGb.toFixed(0)} GB`,
        memoryPercent,
        "oled-reading--capacity"
      )}
      <div class="oled-metrics">
        ${metric("Temperature", formatTemperature(state.cpuTemperatureCelsius))}
        ${metric("Upload", `${state.networkUploadMbps.toFixed(1)} Mbps`)}
        ${metric("Download", `${state.networkDownloadMbps.toFixed(1)} Mbps`)}
      </div>
    </section>
  `;
}

export function renderOledGpuWidget(state: GpuSummary): string {
  const vramPercent = percentage(state.vramUsedGb, state.vramTotalGb);
  return `
    <section class="oled-zone oled-metric" style="--oled-accent:var(--oled-blue)" aria-label="GPU monitor">
      <span class="oled-title"><i data-lucide="microchip"></i>Graphics</span>
      <div class="oled-primary">${Math.round(state.gpuUsagePercent)}<small>%</small></div>
      <span class="oled-primary-label">GPU load</span>
      ${reading(
        "Current utilization",
        `${Math.round(state.gpuUsagePercent)} of 100`,
        state.gpuUsagePercent
      )}
      ${reading(
        "VRAM",
        `${state.vramUsedGb.toFixed(1)} / ${state.vramTotalGb.toFixed(0)} GB`,
        vramPercent,
        "oled-reading--capacity"
      )}
      <div class="oled-metrics">
        ${metric("Temperature", formatTemperature(state.gpuTemperatureCelsius))}
        ${metric("Power", formatReading(state.gpuPowerWatts, "W"))}
        ${metric("Fans", formatReading(state.gpuFanRpm, "RPM"))}
      </div>
    </section>
  `;
}

export function renderOledHardwareWidget(
  system: TelemetrySummary,
  gpu: GpuSummary
): string {
  return `
    <section class="oled-hardware" aria-label="System and graphics monitor">
      ${renderOledSystemWidget(system)}
      ${renderOledGpuWidget(gpu)}
    </section>
  `;
}

export function renderOledMediaWidget(
  state: MediaSummary,
  isCompact = false
): string {
  const artwork = state.artworkDataUrl
    ? `
      <img class="oled-media__backdrop" src="${escapeHtml(state.artworkDataUrl)}" alt="">
      <div class="oled-media__art-wrap">
        <img class="oled-media__art" src="${escapeHtml(state.artworkDataUrl)}" alt="">
      </div>`
    : `
      <div class="oled-media__art-wrap">
        <div class="oled-media__art oled-media__fallback">OP</div>
      </div>`;
  const duration = Math.max(1, state.durationSeconds);
  const artist = state.artist || state.albumArtist || "No artist";
  const isIdle = state.playbackStatus === "Closed";
  const sessionLabel = isIdle
    ? state.source
    : `${state.source} ${state.isPlaying ? "playing" : state.playbackStatus.toLowerCase()}`;

  if (isCompact) {
    return `
      <section class="oled-zone oled-media oled-media--compact" aria-label="Media">
        ${artwork}
        ${mediaSizeButton(true)}
        <div class="oled-media-compact__copy">
          <span class="oled-live${isIdle ? " is-idle" : ""}">${escapeHtml(sessionLabel)}</span>
          <h1>${escapeHtml(state.title)}</h1>
          <p>${escapeHtml(artist)}</p>
        </div>
        <div class="oled-media-compact__transport">
          <button type="button" data-command="media-previous" title="Previous" aria-label="Previous" ${state.canGoPrevious ? "" : "disabled"}><i data-lucide="skip-back"></i></button>
          <button type="button" data-command="media-toggle" title="${state.isPlaying ? "Pause" : "Play"}" aria-label="${state.isPlaying ? "Pause" : "Play"}" ${state.canToggle ? "" : "disabled"}><i data-lucide="${state.isPlaying ? "pause" : "play"}"></i></button>
          <button type="button" data-command="media-next" title="Next" aria-label="Next" ${state.canGoNext ? "" : "disabled"}><i data-lucide="skip-forward"></i></button>
        </div>
      </section>
    `;
  }

  return `
    <section class="oled-zone oled-media" aria-label="Media">
      ${artwork}
      ${mediaSizeButton(false)}
      <div class="oled-media__copy">
        <span class="oled-live${isIdle ? " is-idle" : ""}">${escapeHtml(sessionLabel)}</span>
        <h1>${escapeHtml(state.title)}</h1>
        <p>${escapeHtml(artist)}</p>
        ${state.album ? `<p class="oled-media__album">${escapeHtml(state.album)}</p>` : ""}
        <div class="oled-media__timeline">
          <input
            class="oled-media__progress"
            type="range"
            min="0"
            max="${duration}"
            value="${Math.min(duration, Math.max(0, state.positionSeconds))}"
            data-command="media-seek"
            aria-label="Playback position"
            ${state.canSeek ? "" : "disabled"}>
          <div class="oled-media__time">
            <span>${formatTime(state.positionSeconds)}</span>
            <span>${formatTime(state.durationSeconds)}</span>
          </div>
        </div>
        <div class="oled-transport">
          <button type="button" data-command="media-previous" title="Previous" aria-label="Previous" ${state.canGoPrevious ? "" : "disabled"}><i data-lucide="skip-back"></i></button>
          <button class="oled-transport__play" type="button" data-command="media-toggle" title="${state.isPlaying ? "Pause" : "Play"}" aria-label="${state.isPlaying ? "Pause" : "Play"}" ${state.canToggle ? "" : "disabled"}><i data-lucide="${state.isPlaying ? "pause" : "play"}"></i></button>
          <button type="button" data-command="media-next" title="Next" aria-label="Next" ${state.canGoNext ? "" : "disabled"}><i data-lucide="skip-forward"></i></button>
          <button class="${state.isShuffleActive ? "is-active" : ""}" type="button" data-command="media-shuffle" title="${state.isShuffleActive ? "Turn shuffle off" : "Turn shuffle on"}" aria-label="${state.isShuffleActive ? "Turn shuffle off" : "Turn shuffle on"}" aria-pressed="${state.isShuffleActive === true}" ${state.canShuffle ? "" : "disabled"}><i data-lucide="shuffle"></i></button>
        </div>
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

export function renderOledAudioWidget(state: AudioSummary): string {
  const outputs = state.outputs.slice(0, 5);
  return `
    <section class="oled-zone oled-audio" aria-label="Audio output">
      <header class="oled-audio__head">
        <span class="oled-title"><i data-lucide="audio-lines"></i>Audio</span>
        <div class="audio-center__heading">
          <strong>${state.volumePercent}%</strong>
          ${renderExpandButton()}
        </div>
      </header>
      <div class="audio-center__body">
        <div class="audio-center__playback">
          <div class="oled-audio__current">
            <i data-lucide="volume-2"></i>
            <div>
              <span>Current output</span>
              <strong>${escapeHtml(state.currentOutput)}</strong>
            </div>
          </div>
          <div class="oled-outputs">
            ${outputs.length > 0
              ? outputs.map((output) => {
                  const isActive =
                    output.id === state.currentOutputId || output.isDefault;
                  return `
                    <button
                      type="button"
                      class="oled-output${isActive ? " is-active" : ""}"
                      data-output-id="${escapeHtml(output.id)}"
                      title="${escapeHtml(output.name)}">
                      <i data-lucide="${output.name.toLowerCase().includes("bose") ? "headphones" : "monitor-speaker"}"></i>
                      <span>${escapeHtml(output.name)}</span>
                      <span class="oled-output__dot" aria-hidden="true"></span>
                    </button>
                  `;
                }).join("")
              : `<span class="oled-audio__empty">No active outputs</span>`}
          </div>
          <div class="oled-volume">
            <button type="button" data-command="audio-mute" title="${state.isMuted ? "Unmute" : "Mute"}" aria-label="${state.isMuted ? "Unmute" : "Mute"}" aria-pressed="${state.isMuted}"><i data-lucide="${state.isMuted ? "volume-x" : "volume-2"}"></i></button>
            <input
              type="range"
              min="0"
              max="100"
              value="${state.volumePercent}"
              data-command="audio-volume"
              aria-label="Global volume">
          </div>
        </div>
        ${renderAudioExpandedContent(state)}
      </div>
    </section>
  `;
}

function reading(
  label: string,
  value: string,
  percent: number,
  className = ""
): string {
  return `
    <div class="oled-reading ${className}">
      <div class="oled-reading__head"><span>${label}</span><strong>${value}</strong></div>
      <div class="oled-meter" role="meter" aria-label="${label}" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${Math.round(clampPercent(percent))}">
        <span style="width:${clampPercent(percent)}%"></span>
      </div>
    </div>
  `;
}

function metric(label: string, value: string): string {
  return `<span>${label}<strong>${value}</strong></span>`;
}

function percentage(used: number, total: number): number {
  return total > 0 ? clampPercent(used / total * 100) : 0;
}

function clampPercent(value: number): number {
  return Math.max(0, Math.min(100, value));
}

function formatTemperature(value: number | null): string {
  return value === null ? "--" : `${Math.round(value)} C`;
}

function formatReading(value: number | null, unit: string): string {
  return value === null ? "--" : `${Math.round(value)} ${unit}`;
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
