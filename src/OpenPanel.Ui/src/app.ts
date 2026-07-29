import { postCommand } from "./bridge";
import { renderAudioOutputWidget } from "./widgets/audio-output/audioOutputWidget";
import { renderGpuWidget } from "./widgets/gpu/gpuWidget";
import { renderMediaWidget } from "./widgets/media/mediaWidget";
import { renderSystemWidget } from "./widgets/system/systemWidget";
import type { DashboardState } from "./types";

export function renderDashboard(root: HTMLElement, state: DashboardState): void {
  root.innerHTML = `
    <div class="dashboard" style="--display-width:${state.display.width}; --display-height:${state.display.height};">
      ${renderSystemWidget(state.telemetry)}
      ${renderGpuWidget(state.gpu)}
      ${renderMediaWidget(state.media)}
      ${renderAudioOutputWidget(state.audio)}
    </div>
  `;

  bindCommands(root);
}

function bindCommands(root: HTMLElement): void {
  root.querySelectorAll<HTMLButtonElement>("[data-output-id]").forEach((button) => {
    button.addEventListener("click", () => {
      postCommand({
        type: "command:audio.select",
        payload: { outputId: button.dataset.outputId }
      });
    });
  });

  root.querySelector<HTMLButtonElement>("[data-command='toggle']")?.addEventListener("click", () => {
    postCommand({ type: "command:media.toggle" });
  });
}
