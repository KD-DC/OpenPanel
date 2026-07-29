# OpenPanel

[![CI](https://github.com/KD-DC/OpenPanel/actions/workflows/ci.yml/badge.svg)](https://github.com/KD-DC/OpenPanel/actions/workflows/ci.yml)

OpenPanel is a Windows-first touchscreen dashboard for ultrawide secondary displays, starting with the ASUS ProArt PA147CDV at 1920 x 550.

OpenPanel currently includes a native WPF host, a WebView2 dashboard surface, a TypeScript UI, typed host/UI messaging, live system and storage telemetry, Windows global media controls, and global audio output controls.

## Current Acceptance Target

- Launches as a Windows desktop app.
- Opens a borderless 1920 x 550 dashboard window, preferring a connected display with that resolution.
- Runs without a taskbar button and provides Open and Exit actions from the system tray.
- Renders a swipeable two-page dashboard with nine widgets.
- Updates CPU, GPU, RAM, network, clock, power, fan, thermal, and storage values from the host when supported.
- Shows the preferred active Windows media session with artwork, timeline, and supported playback controls.
- Enumerates active Windows playback endpoints and controls the default output, volume, mute, and peak level.
- Defaults to the Media OLED first-page appearance, with the previous layout available from the tray Appearance menu during evaluation.

## Requirements

- Windows 10 or later.
- .NET 10 SDK with Windows Desktop support.
- Microsoft Edge WebView2 Evergreen Runtime.
- Node.js LTS and npm.

This repo is intended to be developed from native Windows tooling in `C:\dev\openXeneon`. WSL can be used for optional helper commands, but it is not the primary development environment.

## Dependencies

Runtime:

- `LibreHardwareMonitorLib` for read-only CPU, GPU, memory, and storage sensor access.
- `Microsoft.Web.WebView2` for the embedded dashboard surface.
- `NAudio` for maintained Core Audio endpoint, volume, mute, and peak-level wrappers.
- `lucide` for tree-shaken, inline dashboard metric icons.

Development:

- `vite` to bundle the TypeScript dashboard.
- `typescript` for strict UI type checking.
- `@types/node` for Vite's Node-based config types.
- MSTest packages for the initial .NET test project.

No graphing dependency has been added. See `NOTICE.md` for direct and transitive dependency details.

See [`docs/appearance.md`](docs/appearance.md) for the temporary appearance selector and persistence behavior.

## Quick Start

```powershell
.\scripts\setup.ps1
.\scripts\build.ps1
.\scripts\run.ps1
```

Run checks:

```powershell
.\scripts\test.ps1
```

Package a release build:

```powershell
.\scripts\package.ps1
```

## Repository Layout

```text
docs/                 Product and engineering notes
scripts/              Windows PowerShell automation
samples/              Future captured sample payloads
src/OpenPanel.Host/   .NET 10 WPF host
src/OpenPanel.Ui/     TypeScript dashboard UI
```

## Known Limitations

- Individual clock, power, fan, temperature, and VRAM sensors depend on the installed hardware, drivers, and Windows access permissions.
- Media data depends on applications publishing Windows system media sessions.
- Default audio switching uses an isolated Windows compatibility interface because Microsoft does not expose that operation through a fully public API.
- The display chooser is automatic only: it prefers a 1920 x 550 screen, then falls back to the primary display.
- WebView2 and Node/npm must be installed separately.
