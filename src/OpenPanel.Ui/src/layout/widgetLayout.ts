export const pageCapacity = 12;

export const widgetDefinitions = {
  system: { label: "System and graphics", spans: { compact: 2, expanded: 2 } },
  media: { label: "Media", spans: { compact: 2, expanded: 6 } },
  audio: { label: "Audio", spans: { compact: 2, expanded: 2 } },
  memory: { label: "Memory", spans: { compact: 2, expanded: 2 } },
  "cpu-power": { label: "CPU performance", spans: { compact: 2, expanded: 2 } },
  "gpu-power": { label: "GPU performance", spans: { compact: 2, expanded: 2 } },
  "gpu-thermals": { label: "GPU thermals", spans: { compact: 2, expanded: 2 } },
  storage: { label: "Storage", spans: { compact: 4, expanded: 4 } },
  environment: { label: "Weather", spans: { compact: 2, expanded: 2 } },
  peripherals: { label: "Peripheral batteries", spans: { compact: 2, expanded: 2 } },
  gaming: { label: "Gaming performance", spans: { compact: 2, expanded: 2 } }
} as const;

export type WidgetId = keyof typeof widgetDefinitions;
export type WidgetLayout = WidgetId[][];
export type WidgetSize = "compact" | "expanded";
export type WidgetSizes = Record<WidgetId, WidgetSize>;
export interface WidgetLayoutState {
  pages: WidgetLayout;
  sizes: WidgetSizes;
}

const storageKey = "openpanel.widget-layout.v3";
const previousStorageKey = "openpanel.widget-layout.v2";
const legacyStorageKey = "openpanel.widget-layout.v1";
const widgetIds = Object.keys(widgetDefinitions) as WidgetId[];
const defaultLayout: WidgetLayout = [
  ["system", "media", "audio"],
  ["memory", "cpu-power", "gpu-power", "gpu-thermals", "storage"],
  ["environment", "peripherals", "gaming"]
];

export function loadWidgetLayoutState(): WidgetLayoutState {
  try {
    const current = localStorage.getItem(storageKey);
    if (current) {
      const parsed = JSON.parse(current) as {
        pages?: unknown;
        sizes?: unknown;
      };
      const sizes = validateSizes(parsed.sizes);
      return {
        pages: validateLayout(parsed.pages, sizes),
        sizes
      };
    }

    const previous = localStorage.getItem(previousStorageKey);
    if (previous) {
      const parsed = JSON.parse(previous) as {
        pages?: unknown;
        sizes?: unknown;
      };
      const sizes = validateSizes(parsed.sizes);
      const pages = validateLayout(parsed.pages, sizes);
      return {
        pages: packWidgets(pages.flat(), sizes),
        sizes
      };
    }

    const legacy = localStorage.getItem(legacyStorageKey);
    const sizes = defaultSizes();
    return {
      pages: validateLayout(legacy ? JSON.parse(legacy) : defaultLayout, sizes),
      sizes
    };
  } catch {
    return { pages: cloneLayout(defaultLayout), sizes: defaultSizes() };
  }
}

export function saveWidgetLayoutState(state: WidgetLayoutState): void {
  localStorage.setItem(storageKey, JSON.stringify({
    pages: cleanupLayout(state.pages),
    sizes: state.sizes
  }));
}

export function widgetSpan(widgetId: WidgetId, sizes: WidgetSizes): number {
  return widgetDefinitions[widgetId].spans[sizes[widgetId]];
}

export function moveWidget(
  layout: WidgetLayout,
  widgetId: WidgetId,
  targetPageIndex: number,
  targetWidgetIndex: number,
  sizes: WidgetSizes
): WidgetLayout {
  const next = cloneLayout(layout);
  removeWidget(next, widgetId);
  while (next.length <= targetPageIndex) {
    next.push([]);
  }

  const targetPage = next[targetPageIndex]!;
  const insertionIndex = Math.max(0, Math.min(targetPage.length, targetWidgetIndex));
  targetPage.splice(insertionIndex, 0, widgetId);
  carryOverflow(next, targetPageIndex, widgetId, sizes);
  return next;
}

export function resizeWidget(
  layout: WidgetLayout,
  widgetId: WidgetId,
  size: WidgetSize,
  sizes: WidgetSizes
): WidgetLayoutState {
  const nextSizes = { ...sizes, [widgetId]: size };
  return {
    pages: packWidgets(layout.flat(), nextSizes),
    sizes: nextSizes
  };
}

export function cleanupLayout(layout: WidgetLayout): WidgetLayout {
  const cleaned = layout
    .filter((page) => page.length > 0)
    .map((page) => [...page]);
  return cleaned.length > 0 ? cleaned : [[]];
}

export function findWidgetPage(layout: WidgetLayout, widgetId: WidgetId): number {
  return Math.max(0, layout.findIndex((page) => page.includes(widgetId)));
}

export function pageWeight(page: WidgetId[], sizes: WidgetSizes): number {
  return page.reduce(
    (total, widgetId) => total + widgetSpan(widgetId, sizes),
    0
  );
}

function carryOverflow(
  layout: WidgetLayout,
  pageIndex: number,
  protectedWidget: WidgetId,
  sizes: WidgetSizes
): void {
  for (let index = pageIndex; index < layout.length; index++) {
    const page = layout[index]!;
    while (pageWeight(page, sizes) > pageCapacity) {
      let overflowIndex = page.length - 1;
      if (page[overflowIndex] === protectedWidget && page.length > 1) {
        overflowIndex -= 1;
      }

      const overflow = page.splice(overflowIndex, 1)[0]!;
      if (!layout[index + 1]) {
        layout.push([]);
      }
      layout[index + 1]!.unshift(overflow);
    }
  }
}

function validateLayout(value: unknown, sizes: WidgetSizes): WidgetLayout {
  if (!Array.isArray(value)) {
    return cloneLayout(defaultLayout);
  }

  const known = new Set<WidgetId>();
  const parsed: WidgetLayout = [];
  for (const candidatePage of value) {
    if (!Array.isArray(candidatePage)) {
      continue;
    }

    const page: WidgetId[] = [];
    for (const candidateWidget of candidatePage) {
      const migratedWidget = candidateWidget === "gpu"
        ? "system"
        : candidateWidget;
      if (
        typeof migratedWidget === "string" &&
        migratedWidget in widgetDefinitions &&
        !known.has(migratedWidget as WidgetId)
      ) {
        const widgetId = migratedWidget as WidgetId;
        known.add(widgetId);
        page.push(widgetId);
      }
    }
    if (page.length > 0) {
      parsed.push(page);
    }
  }

  const layout = parsed.length > 0 ? parsed : [[]];
  for (const widgetId of widgetIds) {
    if (known.has(widgetId)) {
      continue;
    }

    let targetPage = layout.length - 1;
    if (
      pageWeight(layout[targetPage]!, sizes) + widgetSpan(widgetId, sizes) >
      pageCapacity
    ) {
      layout.push([]);
      targetPage += 1;
    }
    layout[targetPage]!.push(widgetId);
  }

  for (let index = 0; index < layout.length; index++) {
    carryOverflow(layout, index, widgetIds[0]!, sizes);
  }
  return cleanupLayout(layout);
}

function validateSizes(value: unknown): WidgetSizes {
  const defaults = defaultSizes();
  if (!value || typeof value !== "object") {
    return defaults;
  }

  for (const widgetId of widgetIds) {
    const candidate = (value as Partial<WidgetSizes>)[widgetId];
    if (candidate === "compact" || candidate === "expanded") {
      defaults[widgetId] = candidate;
    }
  }
  return defaults;
}

function defaultSizes(): WidgetSizes {
  return Object.fromEntries(
    widgetIds.map((widgetId) => [
      widgetId,
      widgetId === "media" ? "expanded" : "compact"
    ])
  ) as WidgetSizes;
}

function removeWidget(layout: WidgetLayout, widgetId: WidgetId): void {
  for (const page of layout) {
    const index = page.indexOf(widgetId);
    if (index >= 0) {
      page.splice(index, 1);
    }
  }
}

function packWidgets(widgets: WidgetId[], sizes: WidgetSizes): WidgetLayout {
  const pages: WidgetLayout = [[]];
  for (const widgetId of widgets) {
    let page = pages[pages.length - 1]!;
    if (pageWeight(page, sizes) + widgetSpan(widgetId, sizes) > pageCapacity) {
      page = [];
      pages.push(page);
    }
    page.push(widgetId);
  }
  return pages;
}

function cloneLayout(layout: WidgetLayout): WidgetLayout {
  return layout.map((page) => [...page]);
}
