import "./styles/tokens.css";
import "./styles/layout.css";
import "./styles/widgets.css";
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

  currentState = message.payload;
  renderDashboard(root, currentState);
});

startBridge();
