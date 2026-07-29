import { postCommand } from "./bridge";
import { renderAudioOutputWidget } from "./widgets/audio-output/audioOutputWidget";
import { renderGpuWidget } from "./widgets/gpu/gpuWidget";
import { renderMediaWidget } from "./widgets/media/mediaWidget";
import { renderSystemWidget } from "./widgets/system/systemWidget";
import {
  renderCpuPowerWidget,
  renderGpuPowerWidget,
  renderGpuThermalsWidget,
  renderMemoryWidget
} from "./widgets/advanced/advancedWidgets";
import type { DashboardState } from "./types";

export function renderDashboard(root: HTMLElement, state: DashboardState): void {
  if (!root.querySelector(".dashboard-pager")) {
    root.innerHTML = `
      <main class="dashboard-pager" aria-label="OpenPanel dashboard pages" tabindex="0">
        <div class="dashboard dashboard-page" data-page="0">
          <div class="widget-slot" data-widget="system"></div>
          <div class="widget-slot" data-widget="gpu"></div>
          <div class="widget-slot" data-widget="media"></div>
          <div class="widget-slot" data-widget="audio"></div>
        </div>
        <div class="dashboard dashboard-page dashboard-page--advanced" data-page="1">
          <div class="widget-slot" data-widget="memory"></div>
          <div class="widget-slot" data-widget="cpu-power"></div>
          <div class="widget-slot" data-widget="gpu-power"></div>
          <div class="widget-slot" data-widget="gpu-thermals"></div>
        </div>
      </main>
      <nav class="page-indicator" aria-label="Dashboard pages">
        <button class="page-indicator__dot is-active" type="button" data-page-target="0" aria-label="System overview" aria-current="page"></button>
        <button class="page-indicator__dot" type="button" data-page-target="1" aria-label="Power user telemetry"></button>
      </nav>
    `;
    bindPager(root);
  }

  root.style.setProperty("--display-width", `${state.display.width}px`);
  root.style.setProperty("--display-height", `${state.display.height}px`);

  updateWidget(root, "system", renderSystemWidget(state.telemetry));
  updateWidget(root, "gpu", renderGpuWidget(state.gpu));
  updateWidget(root, "media", renderMediaWidget(state.media));
  updateWidget(root, "audio", renderAudioOutputWidget(state.audio));
  updateWidget(root, "memory", renderMemoryWidget(state.advanced));
  updateWidget(root, "cpu-power", renderCpuPowerWidget(state.advanced, state.telemetry));
  updateWidget(root, "gpu-power", renderGpuPowerWidget(state.advanced));
  updateWidget(root, "gpu-thermals", renderGpuThermalsWidget(state.advanced));

  bindCommands(root);
}

function updateWidget(root: HTMLElement, name: string, markup: string): void {
  const slot = root.querySelector<HTMLElement>(`[data-widget="${name}"]`);
  if (slot) {
    slot.innerHTML = markup;
  }
}

function bindPager(root: HTMLElement): void {
  const pager = root.querySelector<HTMLElement>(".dashboard-pager");
  if (!pager) {
    return;
  }

  const showPage = (page: number): void => {
    pager.scrollTo({ left: page * pager.clientWidth, behavior: "smooth" });
  };

  root.querySelectorAll<HTMLButtonElement>("[data-page-target]").forEach((button) => {
    button.addEventListener("click", () => showPage(Number(button.dataset.pageTarget)));
  });

  pager.addEventListener("keydown", (event) => {
    if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
      event.preventDefault();
      showPage(event.key === "ArrowRight" ? 1 : 0);
    }
  });

  let dragStartX: number | null = null;
  let dragStartScrollLeft = 0;
  pager.addEventListener("pointerdown", (event) => {
    if (event.pointerType !== "mouse" || event.button !== 0) {
      return;
    }

    dragStartX = event.clientX;
    dragStartScrollLeft = pager.scrollLeft;
    pager.setPointerCapture(event.pointerId);
    pager.classList.add("is-dragging");
  });
  pager.addEventListener("pointermove", (event) => {
    if (dragStartX === null) {
      return;
    }

    pager.scrollLeft = dragStartScrollLeft - (event.clientX - dragStartX);
  });
  pager.addEventListener("pointerup", (event) => {
    if (dragStartX === null) {
      return;
    }

    dragStartX = null;
    pager.releasePointerCapture(event.pointerId);
    pager.classList.remove("is-dragging");
    showPage(Math.round(pager.scrollLeft / Math.max(1, pager.clientWidth)));
  });

  let updateQueued = false;
  pager.addEventListener("scroll", () => {
    if (updateQueued) {
      return;
    }

    updateQueued = true;
    requestAnimationFrame(() => {
      const currentPage = Math.round(pager.scrollLeft / Math.max(1, pager.clientWidth));
      root.querySelectorAll<HTMLButtonElement>("[data-page-target]").forEach((button) => {
        const isCurrent = Number(button.dataset.pageTarget) === currentPage;
        button.classList.toggle("is-active", isCurrent);
        if (isCurrent) {
          button.setAttribute("aria-current", "page");
        } else {
          button.removeAttribute("aria-current");
        }
      });
      updateQueued = false;
    });
  }, { passive: true });
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
