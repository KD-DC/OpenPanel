# Architecture

OpenPanel uses a thin Windows host with a bundled web dashboard.

## Host

The host is a .NET 10 WPF app. It is responsible for:

- Selecting and positioning the dashboard window.
- Hosting WebView2.
- Sending normalized state to the dashboard UI.
- Receiving typed commands from the dashboard UI.
- Owning Windows-specific services.
- Persisting disabled widget IDs and generating the native tray configuration
  from the central `WidgetCatalog`.

The host samples hardware sensors through LibreHardwareMonitor. RAM and network rates use Windows and .NET APIs. It reads global media sessions through `GlobalSystemMediaTransportControlsSessionManager` and global output state through Core Audio/NAudio. Weather and air-quality responses are fetched through `HttpClient`, normalized in the host, and cached for 15 minutes. The normalized snapshot is sent to the UI once per second.

Default audio output switching is isolated under `Interop/AudioPolicyConfig` because Windows exposes endpoint enumeration and volume publicly but not the default-output setter. Media artwork is cached by track identity and sent only when it changes or after a 30-second refresh.

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

UI-to-host command messages use a `command:*` type and optional payload. Implemented commands cover output selection, volume, mute, media play/pause, previous, next, and seek.

## Resource Use

The UI avoids React and graphing libraries. A single non-overlapping one-second loop collects telemetry, audio, and media state concurrently. LibreHardwareMonitor enables only CPU, GPU, and memory categories. The application-usage sampler enumerates processes at most once every two seconds and computes CPU deltas plus working-set memory only while Hardware application usage is expanded; closing the view clears its baseline and cached rankings. Per-process network traffic uses a real-time ETW session only while Network diagnostics is expanded; the session is stopped immediately when that view closes. If the kernel network provider denies access, the UI offers a deliberate one-time permission action that launches the same executable with `runas`, grants the current user only provider-enable access, exits, and retries from the non-admin dashboard process. Missing sensors or platform sessions are represented as unavailable states rather than retried aggressively.

Extended audio sessions are queried only while the expanded Audio Control Center
is visible. Weather returns its cached snapshot between 15-minute refreshes and
backs off for five minutes after a failure.

Future services should keep coarse update intervals, avoid repeated large payloads, and isolate Windows interop behind small interfaces.
