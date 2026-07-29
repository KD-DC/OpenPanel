# Product Brief

OpenPanel is a Windows-native touchscreen dashboard for ultrawide secondary displays. The first supported target is the ASUS ProArt PA147CDV at 1920 x 550.

The product should be useful before it is customizable. The initial experience is a fixed, deterministic dashboard with large touch targets, readable dense widgets, and no required account login.

## First Milestone Scope

- Native WPF application shell.
- WebView2 dashboard surface.
- TypeScript UI with four placeholder widgets.
- Host-to-UI sample state update.
- UI-to-host command messages for future controls.
- Stub services for display, telemetry, media sessions, audio devices, and settings.

## Deferred

- Live hardware telemetry.
- Windows media session control.
- Global audio output switching.
- Layout editing.
- Installer and startup integration.
