import type { HostToUiMessage, UiToHostMessage } from "./types";

type MessageHandler = (message: HostToUiMessage) => void;

interface WebViewHost {
  postMessage(message: UiToHostMessage): void;
  addEventListener(eventName: "message", handler: (event: { data: unknown }) => void): void;
}

declare global {
  interface Window {
    chrome?: {
      webview?: WebViewHost;
    };
  }
}

const handlers = new Set<MessageHandler>();

export function startBridge(): void {
  window.chrome?.webview?.addEventListener("message", (event) => {
    const message = parseHostMessage(event.data);
    if (!message) {
      return;
    }

    for (const handler of handlers) {
      handler(message);
    }
  });

  postCommand({ type: "command:system.ready" });
}

export function onHostMessage(handler: MessageHandler): () => void {
  handlers.add(handler);
  return () => handlers.delete(handler);
}

export function postCommand(message: UiToHostMessage): void {
  window.chrome?.webview?.postMessage(message);
}

function parseHostMessage(value: unknown): HostToUiMessage | null {
  if (!value || typeof value !== "object") {
    return null;
  }

  const candidate = value as Partial<HostToUiMessage>;
  if (candidate.type !== "state:update" || typeof candidate.payload !== "object") {
    return null;
  }

  return candidate as HostToUiMessage;
}
