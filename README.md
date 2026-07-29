# OpenPanel

[![CI](https://github.com/KD-DC/OpenPanel/actions/workflows/ci.yml/badge.svg)](https://github.com/KD-DC/OpenPanel/actions/workflows/ci.yml)

OpenPanel is a Windows-first touchscreen dashboard for ultrawide secondary displays, starting with the ASUS ProArt PA147CDV at 1920 x 550.

OpenPanel currently includes a native WPF host, a WebView2 dashboard surface, a TypeScript UI, typed host/UI messaging, and live read-only system telemetry. Media controls and audio output switching remain intentionally stubbed.

## Current Acceptance Target

- Launches as a Windows desktop app.
- Opens a borderless 1920 x 550 dashboard window, preferring a connected display with that resolution.
- Renders a swipeable two-page dashboard with eight widgets.
- Updates CPU, GPU, RAM, network, clock, power, fan, and thermal values from the host once per second when supported.
- Keeps media and audio controls as clearly labeled placeholders.

## Requirements

- Windows 10 or later.
- .NET 10 SDK with Windows Desktop support.
- Microsoft Edge WebView2 Evergreen Runtime.
- Node.js LTS and npm.

This repo is intended to be developed from native Windows tooling in `C:\dev\openXeneon`. WSL can be used for optional helper commands, but it is not the primary development environment.

## Dependencies

Runtime:

- `LibreHardwareMonitorLib` for read-only CPU, GPU, and memory sensor access.
- `Microsoft.Web.WebView2` for the embedded dashboard surface.

Development:

- `vite` to bundle the TypeScript dashboard.
- `typescript` for strict UI type checking.
- `@types/node` for Vite's Node-based config types.
- MSTest packages for the initial .NET test project.

No media, audio switching, graphing, or icon dependencies have been added yet. See `NOTICE.md` for direct and transitive dependency details.

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
- Media and audio data are placeholders.
- The display chooser is automatic only: it prefers a 1920 x 550 screen, then falls back to the primary display.
- WebView2 and Node/npm must be installed separately.
