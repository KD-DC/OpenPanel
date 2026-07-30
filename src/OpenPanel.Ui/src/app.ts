import {
  Activity,
  AudioLines,
  BatteryCharging,
  BatteryMedium,
  Bluetooth,
  Cloud,
  CloudFog,
  CloudLightning,
  CloudRain,
  CloudSnow,
  CloudSun,
  Check,
  CircleAlert,
  Cpu,
  createIcons,
  Database,
  Download,
  Droplets,
  Fan,
  Gauge,
  Gamepad2,
  GripVertical,
  HardDrive,
  Headphones,
  Info,
  Keyboard,
  Leaf,
  MapPin,
  Maximize2,
  MemoryStick,
  Microchip,
  Mic,
  MicOff,
  Minimize2,
  MonitorSpeaker,
  Mouse,
  Network,
  Pause,
  Play,
  Radio,
  RefreshCw,
  Shuffle,
  SkipBack,
  SkipForward,
  Square,
  Sun,
  Thermometer,
  Umbrella,
  Upload,
  Volume2,
  VolumeX,
  Wind,
  Zap
} from "lucide";
import { postCommand } from "./bridge";
import { renderAudioOutputWidget } from "./widgets/audio-output/audioOutputWidget";
import { renderEnvironmentWidget } from "./widgets/environment/environmentWidget";
import { renderMediaWidget } from "./widgets/media/mediaWidget";
import { renderCombinedSystemWidget } from "./widgets/system/combinedSystemWidget";
import { renderPeripheralBatteryWidget } from "./widgets/peripherals/peripheralBatteryWidget";
import { renderGamingPerformanceWidget } from "./widgets/gaming/gamingPerformanceWidget";
import {
  renderOledAudioWidget,
  renderOledMediaWidget
} from "./widgets/oled/mediaOledWidgets";
import {
  renderCpuPowerWidget,
  renderGpuPowerWidget,
  renderGpuThermalsWidget,
  renderMemoryWidget,
  renderStorageWidget
} from "./widgets/advanced/advancedWidgets";
import type { DashboardState } from "./types";
import {
  cleanupLayout,
  findWidgetPage,
  loadWidgetLayoutState,
  moveWidget,
  resizeWidget,
  saveWidgetLayoutState,
  widgetSpan,
  widgetDefinitions,
  type WidgetId,
  type WidgetLayout,
  type WidgetSizes
} from "./layout/widgetLayout";

const longPressDurationMs = 600;
const edgeHoldDurationMs = 650;
const edgeThresholdPx = 110;

const initialLayoutState = loadWidgetLayoutState();
let widgetLayout = initialLayoutState.pages;
let widgetSizes: WidgetSizes = initialLayoutState.sizes;
let activePage = 0;
let latestState: DashboardState | null = null;
let isManaging = false;
let draggedWidget: WidgetId | null = null;
let dragPointerId: number | null = null;
let pendingLongPress: number | null = null;
let pendingPointer:
  | { pointerId: number; widgetId: WidgetId; startX: number; startY: number }
  | null = null;
let edgeTimer: number | null = null;
let edgeDirection = 0;
let suppressNextClick = false;

export function renderDashboard(root: HTMLElement, state: DashboardState): void {
  latestState = state;
  const appearance = state.appearance.theme;
  const appearanceChanged = root.dataset.appearance !== appearance;
  if (appearanceChanged) {
    root.dataset.appearance = appearance;
  }

  if (appearanceChanged || !root.querySelector(".dashboard-pager")) {
    renderShell(root);
  }

  root.style.setProperty("--display-width", `${state.display.width}px`);
  root.style.setProperty("--display-height", `${state.display.height}px`);
  renderWidgets(root, state);
  renderIcons();
  bindCommands(root);
  bindWidgetManagement(root);
  syncManagementState(root);
}

function renderShell(root: HTMLElement): void {
  activePage = Math.max(0, Math.min(activePage, widgetLayout.length - 1));
  const oledClass = root.dataset.appearance === "mediaOled"
    ? "dashboard-page--oled"
    : "";
  root.innerHTML = `
    <div class="manage-mode-bar" role="status" aria-live="polite">
      <span><i data-lucide="grip-vertical"></i>Arrange widgets</span>
      <button
        type="button"
        data-command="layout-done"
        title="Finish arranging"
        aria-label="Finish arranging">
        <i data-lucide="check"></i>
      </button>
    </div>
    <main class="dashboard-pager" aria-label="OpenPanel dashboard pages" tabindex="0">
      ${widgetLayout.map((page, pageIndex) => `
        <div class="dashboard dashboard-page ${oledClass}" data-page="${pageIndex}">
          ${page.map((widgetId) => `
            <div
              class="widget-slot"
              data-widget="${widgetId}"
              data-widget-label="${widgetDefinitions[widgetId].label}"
              data-widget-size="${widgetSizes[widgetId]}"
              style="--widget-span:${widgetSpan(widgetId, widgetSizes)}"></div>
          `).join("")}
        </div>
      `).join("")}
    </main>
    <nav class="page-indicator" aria-label="Dashboard pages">
      ${widgetLayout.map((_, pageIndex) => `
        <button
          class="page-indicator__dot ${pageIndex === activePage ? "is-active" : ""}"
          type="button"
          data-page-target="${pageIndex}"
          aria-label="Dashboard page ${pageIndex + 1}"
          ${pageIndex === activePage ? 'aria-current="page"' : ""}></button>
      `).join("")}
    </nav>
  `;
  bindPager(root);
  const pager = root.querySelector<HTMLElement>(".dashboard-pager");
  if (pager) {
    pager.scrollLeft = activePage * pager.clientWidth;
  }
}

function renderWidgets(root: HTMLElement, state: DashboardState): void {
  const isMediaOled = state.appearance.theme === "mediaOled";
  updateWidget(
    root,
    "system",
    renderCombinedSystemWidget(state.telemetry, state.gpu, state.network)
  );
  updateWidget(
    root,
    "media",
    isMediaOled
      ? renderOledMediaWidget(state.media, widgetSizes.media === "compact")
      : renderMediaWidget(state.media, widgetSizes.media === "compact")
  );
  updateWidget(
    root,
    "audio",
    isMediaOled
      ? renderOledAudioWidget(state.audio)
      : renderAudioOutputWidget(state.audio)
  );
  updateWidget(root, "memory", renderMemoryWidget(state.advanced));
  updateWidget(root, "cpu-power", renderCpuPowerWidget(state.advanced, state.telemetry));
  updateWidget(root, "gpu-power", renderGpuPowerWidget(state.advanced));
  updateWidget(root, "gpu-thermals", renderGpuThermalsWidget(state.advanced));
  updateWidget(root, "storage", renderStorageWidget(state.storage));
  updateWidget(root, "environment", renderEnvironmentWidget(state.weather));
  updateWidget(root, "peripherals", renderPeripheralBatteryWidget(state.peripherals));
  updateWidget(root, "gaming", renderGamingPerformanceWidget(state.gaming));
}

function renderIcons(): void {
  createIcons({
    icons: {
      Activity,
      AudioLines,
      BatteryCharging,
      BatteryMedium,
      Bluetooth,
      Check,
      CircleAlert,
      Cloud,
      CloudFog,
      CloudLightning,
      CloudRain,
      CloudSnow,
      CloudSun,
      Cpu,
      Database,
      Download,
      Droplets,
      Fan,
      Gauge,
      Gamepad2,
      GripVertical,
      HardDrive,
      Headphones,
      Info,
      Keyboard,
      Leaf,
      MapPin,
      Maximize2,
      MemoryStick,
      Microchip,
      Mic,
      MicOff,
      Minimize2,
      MonitorSpeaker,
      Mouse,
      Network,
      Pause,
      Play,
      Radio,
      RefreshCw,
      Shuffle,
      SkipBack,
      SkipForward,
      Square,
      Sun,
      Thermometer,
      Umbrella,
      Upload,
      Volume2,
      VolumeX,
      Wind,
      Zap
    },
    attrs: {
      "aria-hidden": "true"
    }
  });
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
  const focusedInputId = activeElement?.dataset.inputId;
  const focusedSessionId = activeElement?.dataset.sessionId;
  slot.innerHTML = markup;
  slot.dataset.markup = markup;
  if (name === "audio") {
    syncAudioExpandedState(slot);
  } else if (name === "system") {
    syncNetworkExpandedState(slot);
  } else if (name === "environment") {
    syncEnvironmentExpandedState(slot);
  }

  const replacement =
    (focusedSessionId
      ? Array.from(slot.querySelectorAll<HTMLElement>("[data-session-id]"))
          .find((element) =>
            element.dataset.sessionId === focusedSessionId &&
            element.dataset.command === focusedCommand)
      : null) ??
    (focusedInputId
      ? Array.from(slot.querySelectorAll<HTMLElement>("[data-input-id]"))
          .find((element) => element.dataset.inputId === focusedInputId)
      : null) ??
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
    activePage = Math.max(0, Math.min(widgetLayout.length - 1, page));
    pager.scrollTo({
      left: activePage * pager.clientWidth,
      behavior: isManaging ? "auto" : "smooth"
    });
  };

  root.querySelectorAll<HTMLButtonElement>("[data-page-target]").forEach((button) => {
    button.addEventListener("click", () => {
      showPage(Number(button.dataset.pageTarget));
    });
  });

  pager.addEventListener("keydown", (event) => {
    if (!isManaging && (event.key === "ArrowLeft" || event.key === "ArrowRight")) {
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
  let pagerPointerId: number | null = null;
  let pagerStartedInManagement = false;
  let pagerWasDragged = false;
  pager.addEventListener("pointerdown", (event) => {
    const target = event.target as Element;
    if (
      (event.pointerType !== "mouse" && !isManaging) ||
      event.button !== 0 ||
      (isManaging && target.closest("[data-widget]")) ||
      target.closest(
        "button, input, select, textarea, a, [role='button']"
      )
    ) {
      return;
    }

    pagerStartedInManagement = isManaging;
    pagerWasDragged = false;
    pagerPointerId = event.pointerId;
    dragStartX = event.clientX;
    dragStartScrollLeft = pager.scrollLeft;
  });
  pager.addEventListener("pointermove", (event) => {
    if (
      dragStartX === null ||
      pagerPointerId !== event.pointerId ||
      (isManaging && !pagerStartedInManagement)
    ) {
      return;
    }

    if (!pagerWasDragged && Math.abs(event.clientX - dragStartX) > 12) {
      pagerWasDragged = true;
      suppressNextClick = true;
      pager.setPointerCapture(event.pointerId);
      pager.classList.add("is-dragging");
    }
    if (pagerWasDragged) {
      event.preventDefault();
      pager.scrollLeft = dragStartScrollLeft - (event.clientX - dragStartX);
    }
  });
  const finishPagerDrag = (event: PointerEvent): void => {
    if (dragStartX === null || pagerPointerId !== event.pointerId) {
      return;
    }

    dragStartX = null;
    pagerPointerId = null;
    if (pagerWasDragged && pager.hasPointerCapture(event.pointerId)) {
      pager.releasePointerCapture(event.pointerId);
    }
    pager.classList.remove("is-dragging");
    if (pagerWasDragged && (!isManaging || pagerStartedInManagement)) {
      showPage(Math.round(pager.scrollLeft / Math.max(1, pager.clientWidth)));
    }
    if (pagerWasDragged) {
      window.setTimeout(() => {
        suppressNextClick = false;
      }, 80);
    }
    pagerStartedInManagement = false;
    pagerWasDragged = false;
  };
  pager.addEventListener("pointerup", finishPagerDrag);
  pager.addEventListener("pointercancel", finishPagerDrag);

  let updateQueued = false;
  pager.addEventListener("scroll", () => {
    if (updateQueued) {
      return;
    }

    updateQueued = true;
    requestAnimationFrame(() => {
      const currentPage = Math.round(pager.scrollLeft / Math.max(1, pager.clientWidth));
      activePage = Math.max(0, Math.min(widgetLayout.length - 1, currentPage));
      root.querySelectorAll<HTMLButtonElement>("[data-page-target]").forEach((button) => {
        const isCurrent = Number(button.dataset.pageTarget) === activePage;
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
    const command = target.closest<HTMLElement>("[data-command]")?.dataset.command;
    if (command === "layout-done") {
      exitManagement(root);
      return;
    }
    if (isManaging || suppressNextClick) {
      event.preventDefault();
      return;
    }

    const inputButton = target.closest<HTMLButtonElement>("[data-input-id]");
    if (inputButton) {
      postCommand({
        type: "command:audio.input.select",
        payload: { inputId: inputButton.dataset.inputId }
      });
      return;
    }

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

    switch (command) {
      case "audio-expand": {
        const slot = target.closest<HTMLElement>("[data-widget='audio']");
        if (slot) {
          slot.dataset.expanded =
            slot.dataset.expanded === "true" ? "false" : "true";
          syncAudioExpandedState(slot);
          postCommand({
            type: "command:audio.expanded",
            payload: { isExpanded: slot.dataset.expanded === "true" }
          });
        }
        break;
      }
      case "environment-expand": {
        const slot = target.closest<HTMLElement>("[data-widget='environment']");
        if (slot) {
          slot.dataset.expanded =
            slot.dataset.expanded === "true" ? "false" : "true";
          syncEnvironmentExpandedState(slot);
        }
        break;
      }
      case "network-expand": {
        const slot = target.closest<HTMLElement>("[data-widget='system']");
        if (slot) {
          slot.dataset.expanded =
            slot.dataset.expanded === "true" ? "false" : "true";
          syncNetworkExpandedState(slot);
          postCommand({
            type: "command:network.expanded",
            payload: { isExpanded: slot.dataset.expanded === "true" }
          });
        }
        break;
      }
      case "gaming-toggle": {
        const button = target.closest<HTMLButtonElement>("[data-command]");
        postCommand({
          type: "command:gaming.active",
          payload: { isActive: button?.getAttribute("aria-pressed") !== "true" }
        });
        break;
      }
      case "media-size": {
        const resized = resizeWidget(
          widgetLayout,
          "media",
          widgetSizes.media === "compact" ? "expanded" : "compact",
          widgetSizes
        );
        widgetLayout = resized.pages;
        widgetSizes = resized.sizes;
        activePage = findWidgetPage(widgetLayout, "media");
        saveLayout();
        applyWidgetLayout(root);
        break;
      }
      case "audio-mute": {
        const button = target.closest<HTMLButtonElement>("[data-command]");
        const isMuted =
          button?.getAttribute("aria-pressed") === "true" ||
          button?.textContent === "Unmute";
        postCommand({ type: "command:audio.mute", payload: { isMuted: !isMuted } });
        break;
      }
      case "audio-input-mute": {
        const button = target.closest<HTMLButtonElement>("[data-command]");
        postCommand({
          type: "command:audio.input.mute",
          payload: { isMuted: button?.getAttribute("aria-pressed") !== "true" }
        });
        break;
      }
      case "audio-session-mute": {
        const button = target.closest<HTMLButtonElement>("[data-session-id]");
        if (button?.dataset.sessionId) {
          postCommand({
            type: "command:audio.session.mute",
            payload: {
              sessionId: button.dataset.sessionId,
              isMuted: button.getAttribute("aria-pressed") !== "true"
            }
          });
        }
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
      case "media-shuffle": {
        const button = target.closest<HTMLButtonElement>("[data-command]");
        postCommand({
          type: "command:media.shuffle",
          payload: { isActive: button?.getAttribute("aria-pressed") !== "true" }
        });
        break;
      }
    }
  });

  root.addEventListener("change", (event) => {
    if (isManaging) {
      event.preventDefault();
      return;
    }

    const input = event.target as HTMLInputElement;
    if (input.dataset.command === "audio-volume") {
      postCommand({
        type: "command:audio.volume",
        payload: { volumePercent: Number(input.value) }
      });
    } else if (input.dataset.command === "audio-input-volume") {
      postCommand({
        type: "command:audio.input.volume",
        payload: { volumePercent: Number(input.value) }
      });
    } else if (
      input.dataset.command === "audio-session-volume" &&
      input.dataset.sessionId
    ) {
      postCommand({
        type: "command:audio.session.volume",
        payload: {
          sessionId: input.dataset.sessionId,
          volumePercent: Number(input.value)
        }
      });
    } else if (input.dataset.command === "media-seek") {
      postCommand({
        type: "command:media.seek",
        payload: { positionSeconds: Number(input.value) }
      });
    }
  });
}

function bindWidgetManagement(root: HTMLElement): void {
  if (root.dataset.managementBound === "true") {
    return;
  }

  root.dataset.managementBound = "true";
  root.addEventListener("pointerdown", (event) => {
    if (event.button !== 0) {
      return;
    }

    const slot = (event.target as Element).closest<HTMLElement>("[data-widget]");
    const widgetId = slot?.dataset.widget;
    if (!slot || !isWidgetId(widgetId)) {
      return;
    }

    if (isManaging) {
      event.preventDefault();
      beginWidgetDrag(root, widgetId, event.pointerId);
      return;
    }

    clearLongPress();
    pendingPointer = {
      pointerId: event.pointerId,
      widgetId,
      startX: event.clientX,
      startY: event.clientY
    };
    pendingLongPress = window.setTimeout(() => {
      if (!pendingPointer || pendingPointer.pointerId !== event.pointerId) {
        return;
      }

      beginWidgetDrag(root, widgetId, event.pointerId);
      pendingPointer = null;
      pendingLongPress = null;
    }, longPressDurationMs);
  }, { capture: true });

  root.addEventListener("pointermove", (event) => {
    if (
      pendingPointer &&
      pendingPointer.pointerId === event.pointerId &&
      Math.hypot(
        event.clientX - pendingPointer.startX,
        event.clientY - pendingPointer.startY
      ) > 12
    ) {
      clearLongPress();
    }

    if (dragPointerId !== event.pointerId || !draggedWidget) {
      return;
    }

    event.preventDefault();
    reorderAtPoint(root, event.clientX, event.clientY);
    scheduleEdgeMove(root, event.clientX);
  }, { capture: true });

  const finishPointer = (event: PointerEvent): void => {
    if (pendingPointer?.pointerId === event.pointerId) {
      clearLongPress();
    }
    if (dragPointerId === event.pointerId) {
      finishWidgetDrag(root);
    }
  };
  root.addEventListener("pointerup", finishPointer, { capture: true });
  root.addEventListener("pointercancel", finishPointer, { capture: true });
}

function beginWidgetDrag(
  root: HTMLElement,
  widgetId: WidgetId,
  pointerId: number
): void {
  isManaging = true;
  draggedWidget = widgetId;
  dragPointerId = pointerId;
  suppressNextClick = true;
  clearEdgeTimer();
  root.querySelectorAll<HTMLElement>("[data-expanded='true']").forEach((slot) => {
    slot.dataset.expanded = "false";
    if (slot.dataset.widget === "audio") {
      syncAudioExpandedState(slot);
    } else if (slot.dataset.widget === "environment") {
      syncEnvironmentExpandedState(slot);
    }
  });
  syncManagementState(root);
  try {
    root.setPointerCapture(pointerId);
  } catch {
    // Pointer capture can already belong to the pager on a mouse long-press.
  }
}

function finishWidgetDrag(root: HTMLElement): void {
  const completedWidget = draggedWidget;
  const completedPointer = dragPointerId;
  clearEdgeTimer();
  draggedWidget = null;
  dragPointerId = null;
  widgetLayout = cleanupLayout(widgetLayout);
  if (completedWidget) {
    activePage = findWidgetPage(widgetLayout, completedWidget);
  }
  saveLayout();
  applyWidgetLayout(root);
  if (completedPointer !== null && root.hasPointerCapture(completedPointer)) {
    root.releasePointerCapture(completedPointer);
  }
  window.setTimeout(() => {
    suppressNextClick = false;
  }, 80);
}

function reorderAtPoint(root: HTMLElement, clientX: number, clientY: number): void {
  if (!draggedWidget) {
    return;
  }

  const targetSlot = document
    .elementFromPoint(clientX, clientY)
    ?.closest<HTMLElement>("[data-widget]");
  const targetWidget = targetSlot?.dataset.widget;
  const targetPageElement = targetSlot?.closest<HTMLElement>("[data-page]");
  if (
    !targetSlot ||
    !isWidgetId(targetWidget) ||
    targetWidget === draggedWidget ||
    !targetPageElement
  ) {
    return;
  }

  const targetPage = Number(targetPageElement.dataset.page);
  const targetIndex = widgetLayout[targetPage]?.indexOf(targetWidget) ?? -1;
  const sourcePage = findWidgetPage(widgetLayout, draggedWidget);
  const sourceIndex = widgetLayout[sourcePage]?.indexOf(draggedWidget) ?? -1;
  if (targetIndex < 0 || sourceIndex < 0) {
    return;
  }

  const targetRect = targetSlot.getBoundingClientRect();
  let insertionIndex = targetIndex + (clientX > targetRect.x + targetRect.width / 2 ? 1 : 0);
  if (sourcePage === targetPage && sourceIndex < insertionIndex) {
    insertionIndex -= 1;
  }
  if (sourcePage === targetPage && sourceIndex === insertionIndex) {
    return;
  }

  widgetLayout = moveWidget(
    widgetLayout,
    draggedWidget,
    targetPage,
    insertionIndex,
    widgetSizes
  );
  activePage = findWidgetPage(widgetLayout, draggedWidget);
  applyWidgetLayout(root);
}

function scheduleEdgeMove(root: HTMLElement, clientX: number): void {
  const pager = root.querySelector<HTMLElement>(".dashboard-pager");
  if (!pager) {
    return;
  }

  const pagerRect = pager.getBoundingClientRect();
  const direction = clientX <= pagerRect.left + edgeThresholdPx
    ? -1
    : clientX >= pagerRect.right - edgeThresholdPx
      ? 1
      : 0;
  if (direction === edgeDirection) {
    return;
  }

  clearEdgeTimer();
  edgeDirection = direction;
  if (direction === 0 || !draggedWidget) {
    return;
  }

  edgeTimer = window.setTimeout(() => {
    if (!draggedWidget) {
      return;
    }

    const sourcePage = findWidgetPage(widgetLayout, draggedWidget);
    const targetPage = sourcePage + direction;
    if (targetPage < 0) {
      clearEdgeTimer();
      return;
    }

    const nextLayout: WidgetLayout = widgetLayout.map((page) => [...page]);
    while (nextLayout.length <= targetPage) {
      nextLayout.push([]);
    }
    widgetLayout = moveWidget(
      nextLayout,
      draggedWidget,
      targetPage,
      nextLayout[targetPage]!.length,
      widgetSizes
    );
    activePage = findWidgetPage(widgetLayout, draggedWidget);
    clearEdgeTimer();
    applyWidgetLayout(root);
  }, edgeHoldDurationMs);
}

function exitManagement(root: HTMLElement): void {
  clearLongPress();
  clearEdgeTimer();
  draggedWidget = null;
  dragPointerId = null;
  isManaging = false;
  suppressNextClick = false;
  widgetLayout = cleanupLayout(widgetLayout);
  activePage = Math.max(0, Math.min(activePage, widgetLayout.length - 1));
  saveLayout();
  applyWidgetLayout(root);
}

function applyWidgetLayout(root: HTMLElement): void {
  renderShell(root);
  if (latestState) {
    renderWidgets(root, latestState);
  }
  renderIcons();
  syncManagementState(root);
}

function syncManagementState(root: HTMLElement): void {
  root.classList.toggle("is-managing", isManaging);
  root.querySelectorAll<HTMLElement>("[data-widget]").forEach((slot) => {
    slot.classList.toggle(
      "is-widget-dragging",
      isManaging && slot.dataset.widget === draggedWidget
    );
  });
}

function clearLongPress(): void {
  if (pendingLongPress !== null) {
    window.clearTimeout(pendingLongPress);
  }
  pendingLongPress = null;
  pendingPointer = null;
}

function clearEdgeTimer(): void {
  if (edgeTimer !== null) {
    window.clearTimeout(edgeTimer);
  }
  edgeTimer = null;
  edgeDirection = 0;
}

function isWidgetId(value: string | undefined): value is WidgetId {
  return Boolean(value && value in widgetDefinitions);
}

function saveLayout(): void {
  saveWidgetLayoutState({ pages: widgetLayout, sizes: widgetSizes });
}

function syncAudioExpandedState(slot: HTMLElement): void {
  const isExpanded = slot.dataset.expanded === "true";
  const button = slot.querySelector<HTMLButtonElement>("[data-command='audio-expand']");
  button?.setAttribute("aria-expanded", String(isExpanded));
  button?.setAttribute(
    "aria-label",
    isExpanded ? "Collapse audio controls" : "Expand audio controls"
  );
  button?.setAttribute(
    "title",
    isExpanded ? "Collapse audio controls" : "Expand audio controls"
  );
}

function syncEnvironmentExpandedState(slot: HTMLElement): void {
  const isExpanded = slot.dataset.expanded === "true";
  slot.querySelectorAll<HTMLButtonElement>("[data-command='environment-expand']")
    .forEach((button) => {
      const isCollapseButton = button.closest(".environment") !== null;
      button.setAttribute("aria-expanded", String(isExpanded));
      button.tabIndex = isCollapseButton === isExpanded ? 0 : -1;
    });
}

function syncNetworkExpandedState(slot: HTMLElement): void {
  const isExpanded = slot.dataset.expanded === "true";
  slot.querySelectorAll<HTMLButtonElement>("[data-command='network-expand']")
    .forEach((button) => {
      const isCollapseButton = button.closest(".network-quality") !== null;
      button.setAttribute("aria-expanded", String(isExpanded));
      button.tabIndex = isCollapseButton === isExpanded ? 0 : -1;
    });
}
