import "./styles/tokens.css";
import "./styles/layout.css";
import "./styles/widgets.css";
import "./styles/media-oled.css";
import "./styles/environment.css";
import { onHostMessage, startBridge } from "./bridge";
import { renderDashboard } from "./app";
import { initialState } from "./state/store";
import type { DashboardState } from "./types";

const root = document.querySelector<HTMLElement>("#app");

if (!root) {
  throw new Error("OpenPanel root element is missing.");
}

let currentState: DashboardState = initialState;

renderDashboard(root, currentState);

onHostMessage((message) => {
  if (message.type !== "state:update") {
    return;
  }

  const incoming = message.payload;
  const isSameTrack =
    incoming.media.source === currentState.media.source &&
    incoming.media.title === currentState.media.title &&
    incoming.media.artist === currentState.media.artist &&
    incoming.media.album === currentState.media.album;
  currentState = {
    ...incoming,
    media: {
      ...incoming.media,
      artworkDataUrl:
        incoming.media.artworkDataUrl ??
        (isSameTrack ? currentState.media.artworkDataUrl : null)
    }
  };
  renderDashboard(root, currentState);
});

startBridge();
