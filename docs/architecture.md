# Architecture

OpenPanel uses a thin Windows host with a bundled web dashboard.

## Host

The host is a .NET 10 WPF app. It is responsible for:

- Selecting and positioning the dashboard window.
- Hosting WebView2.
- Sending normalized state to the dashboard UI.
- Receiving typed commands from the dashboard UI.
- Owning Windows-specific services.

The first implementation only sends sample state. Telemetry, media, and audio services are stubs so the app shell can be tested before hardware-specific work begins.

## UI

The dashboard UI is TypeScript built by Vite. It intentionally avoids a UI framework for the first milestone to keep runtime overhead low and make the generated dashboard static and predictable.

The UI renders from a single `DashboardState` model and sends commands only through the typed bridge in `src/OpenPanel.Ui/src/bridge.ts`.

## Messaging

Host-to-UI:

```json
{
  "type": "state:update",
  "payload": {
    "telemetry": {},
    "gpu": {},
    "media": {},
    "audio": {},
    "display": {}
  }
}
```

UI-to-host command messages use a `command:*` type and optional payload. Commands are currently logged by the host.

## Resource Use

The first UI avoids React and graphing libraries. The WPF layer does not poll hardware yet. Future services should use coarse update intervals, avoid repeated large payloads, and isolate Windows interop behind small interfaces.
