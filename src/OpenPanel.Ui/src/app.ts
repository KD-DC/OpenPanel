import {
  Activity,
  Cpu,
  createIcons,
  Database,
  Download,
  Fan,
  Gauge,
  HardDrive,
  MemoryStick,
  Microchip,
  Thermometer,
  Upload,
  Zap
} from "lucide";
import { postCommand } from "./bridge";
import { renderAudioOutputWidget } from "./widgets/audio-output/audioOutputWidget";
import { renderGpuWidget } from "./widgets/gpu/gpuWidget";
import { renderMediaWidget } from "./widgets/media/mediaWidget";
import { renderSystemWidget } from "./widgets/system/systemWidget";
import {
  renderCpuPowerWidget,
  renderGpuPowerWidget,
  renderGpuThermalsWidget,
  renderMemoryWidget,
  renderStorageWidget
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
          <div class="widget-slot" data-widget="storage"></div>
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
  updateWidget(root, "storage", renderStorageWidget(state.storage));

  createIcons({
    icons: {
      Activity,
      Cpu,
      Database,
      Download,
      Fan,
      Gauge,
      HardDrive,
      MemoryStick,
      Microchip,
      Thermometer,
      Upload,
      Zap
    },
    attrs: {
      "aria-hidden": "true"
    }
  });
  bindCommands(root);
}

function updateWidget(root: HTMLElement, name: string, markup: string): void {
  const slot = root.querySelector<HTMLElement>(`[data-widget="${name}"]`);
  if (!slot || slot.dataset.markup === markup) {
    return;
  }

  const activeElement = slot.contains(document.activeElement)
    ? document.activeElement as HTMLElement
    : null;
  if (
    activeElement instanceof HTMLInputElement &&
    activeElement.matches(":active")
  ) {
    return;
  }

  const focusedCommand = activeElement?.dataset.command;
  const focusedOutputId = activeElement?.dataset.outputId;
  slot.innerHTML = markup;
  slot.dataset.markup = markup;

  const replacement =
    (focusedCommand
      ? slot.querySelector<HTMLElement>(`[data-command="${focusedCommand}"]`)
      : null) ??
    (focusedOutputId
      ? Array.from(slot.querySelectorAll<HTMLElement>("[data-output-id]"))
          .find((element) => element.dataset.outputId === focusedOutputId)
      : null);
  replacement?.focus({ preventScroll: true });
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
      const currentPage = Math.round(
        pager.scrollLeft / Math.max(1, pager.clientWidth)
      );
      const pageCount = pager.querySelectorAll("[data-page]").length;
      const delta = event.key === "ArrowRight" ? 1 : -1;
      showPage(Math.max(0, Math.min(pageCount - 1, currentPage + delta)));
    }
  });

  let dragStartX: number | null = null;
  let dragStartScrollLeft = 0;
  pager.addEventListener("pointerdown", (event) => {
    const target = event.target as Element;
    if (
      event.pointerType !== "mouse" ||
      event.button !== 0 ||
      target.closest("button, input, label")
    ) {
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
  if (root.dataset.commandsBound === "true") {
    return;
  }

  root.dataset.commandsBound = "true";
  root.addEventListener("click", (event) => {
    const target = event.target as Element;
    const outputButton = target.closest<HTMLButtonElement>("[data-output-id]");
    if (outputButton) {
      postCommand({
        type: "command:audio.select",
        payload: {
          outputId: outputButton.dataset.outputId,
          setCommunicationsDevice:
            root.querySelector<HTMLInputElement>("[data-setting='communications']")?.checked ?? true
        }
      });
      return;
    }

    const command = target.closest<HTMLElement>("[data-command]")?.dataset.command;
    switch (command) {
      case "audio-mute": {
        const isMuted = target.closest("[data-command]")?.textContent === "Unmute";
        postCommand({ type: "command:audio.mute", payload: { isMuted: !isMuted } });
        break;
      }
      case "media-toggle":
        postCommand({ type: "command:media.toggle" });
        break;
      case "media-previous":
        postCommand({ type: "command:media.previous" });
        break;
      case "media-next":
        postCommand({ type: "command:media.next" });
        break;
    }
  });

  root.addEventListener("change", (event) => {
    const input = event.target as HTMLInputElement;
    if (input.dataset.command === "audio-volume") {
      postCommand({
        type: "command:audio.volume",
        payload: { volumePercent: Number(input.value) }
      });
    } else if (input.dataset.command === "media-seek") {
      postCommand({
        type: "command:media.seek",
        payload: { positionSeconds: Number(input.value) }
      });
    }
  });
}
