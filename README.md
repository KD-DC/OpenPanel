# OpenPanel

OpenPanel is a Windows-first touchscreen dashboard for ultrawide secondary displays, starting with the ASUS ProArt PA147CDV at 1920 x 550.

The first milestone establishes the app shell: a native WPF host, a WebView2 dashboard surface, a TypeScript UI, placeholder widgets, and typed host/UI messaging. Full hardware telemetry, media controls, and audio output switching are intentionally stubbed for now.

## Current Acceptance Target

- Launches as a Windows desktop app.
- Opens a borderless 1920 x 550 dashboard window, preferring a connected display with that resolution.
- Renders four placeholder widgets: System, GPU, Media, and Audio Output.
- Sends a sample `state:update` message from the C# host to the TypeScript UI.

## Requirements

- Windows 10 or later.
- .NET 10 SDK with Windows Desktop support.
- Microsoft Edge WebView2 Evergreen Runtime.
- Node.js LTS and npm.

This repo is intended to be developed from native Windows tooling in `C:\dev\openXeneon`. WSL can be used for optional helper commands, but it is not the primary development environment.

## Dependencies

Runtime:

- `Microsoft.Web.WebView2` for the embedded dashboard surface.

Development:

- `vite` to bundle the TypeScript dashboard.
- `typescript` for strict UI type checking.
- `@types/node` for Vite's Node-based config types.
- MSTest packages for the initial .NET test project.

No telemetry, media, audio switching, graphing, or icon dependencies have been added yet.

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

- The current telemetry, media, and audio data are sample placeholders.
- The display chooser is automatic only: it prefers a 1920 x 550 screen, then falls back to the primary display.
- WebView2 and Node/npm must be installed separately.
- The local machine used to create this skeleton did not have the .NET SDK or Node.js on PATH, so build verification requires installing those prerequisites first.
