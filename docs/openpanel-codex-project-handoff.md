# OpenPanel Project Handoff for Codex

Date prepared: July 29, 2026

## 1. Project Summary

Build an open-source, Windows-first touchscreen dashboard inspired by the Corsair XENEON EDGE software experience, but designed for ordinary secondary displays, starting with the ASUS ProArt PA147CDV.

Working project name: **OpenPanel**

Primary goal: create a polished, touch-first Windows dashboard that runs full-screen on the ASUS PA147CDV and provides useful PC controls without requiring Corsair hardware or iCUE.

This should be original software. Do not copy Corsair branding, widget artwork, iconography, names, CSS, layouts, or proprietary behavior. The target is a similar class of experience: glanceable widgets, touch controls, system telemetry, media controls, and quick PC utilities.

## 2. Hardware Target

Primary hardware:

- Display: ASUS ProArt Display PA147CDV
- Resolution: 1920 x 550
- Size: 14 inches
- Aspect ratio: 32:9
- Refresh rate: 60 Hz
- Touch: 10-point capacitive touch
- Inputs: HDMI 1.4 and USB-C with DisplayPort Alt Mode
- Special controls: ASUS Dial and Adobe-oriented Control Panel features

Reference: ASUS lists the PA147CDV as a 14-inch 32:9 IPS display at 1920 x 550 with 10-point touch. See the official ASUS specs: https://www.asus.com/us/displays-desktops/monitors/proart/proart-display-pa147cdv/techspec/

Comparison target:

- Corsair XENEON EDGE: 14.5-inch 2560 x 720 60 Hz LCD touchscreen with 5-point touch, HDMI, USB-C DP Alt Mode, and iCUE/widget positioning.
- Reference: https://www.corsair.com/us/en/p/monitors/cc-9011306-ww/xeneon-edge-14-5-lcd-touchscreen-cc-9011306-ww

Design implication:

- The ASUS has fewer pixels than XENEON EDGE, but its aspect ratio is close enough that a dedicated 1920 x 550 design can feel native.
- Do not design for 16:9. The dashboard must treat 1920 x 550 as the main canvas.
- Use large touch targets and compact information density.

## 3. Target User and Use Cases

Primary user:

- Windows gaming/workstation user with a high-end desktop PC.
- Uses the ASUS PA147CDV as a small secondary touchscreen.
- Wants XENEON-style widgets without buying a XENEON EDGE.
- Wants practical PC controls, not just decorative system stats.

Initial use cases:

- See CPU, GPU, RAM, storage, network, and cooling stats at a glance.
- Control Spotify and other active media apps from the touchscreen.
- Quickly select the global Windows sound output: headphones, desk speakers, HDMI, Bluetooth, virtual audio device, etc.
- Control global volume and mute.
- Keep the dashboard always available on the ASUS display.
- Start with Windows and recover gracefully if the display is disconnected.

Explicit non-goal for MVP:

- Per-application audio routing is not needed.

## 4. Product Principles

- Windows-first, not cross-platform-first.
- Touch-first, not mouse-first.
- Useful before customizable.
- Prefer robust Windows APIs and existing open-source components over custom low-level hardware code.
- Build a clean personal MVP first, then decide whether to harden it into a broader open-source beta.
- Avoid vendor lock-in.
- Avoid requiring account logins for MVP features.
- Avoid a local web server unless clearly needed. Prefer WebView2 host messaging.

## 5. Recommended Architecture

Use a native Windows host with a bundled web dashboard UI.

### Native Host

Recommended stack:

- Language: C#
- Runtime: .NET 10 LTS
- Desktop shell: WPF initially
- Embedded UI: Microsoft Edge WebView2
- Packaging: start with simple `dotnet publish`; add installer later

Why .NET 10:

- .NET 10 is the current LTS release and is supported until November 14, 2028, according to Microsoft. See https://dotnet.microsoft.com/en-us/platform/support/policy
- .NET 8 reaches end of support on November 10, 2026, so new work should start on .NET 10 unless a dependency blocks it.

Why WPF:

- Mature Windows desktop framework.
- Straightforward full-screen borderless windows, tray integration, startup behavior, and Win32 interop.
- The visible dashboard can be HTML/CSS/TypeScript in WebView2, so WPF stays thin.
- WinUI 3 can be revisited later, but it adds packaging and Windows App SDK overhead that is not necessary for the first working version.

Why WebView2:

- Faster UI iteration than native XAML for custom dashboards.
- Good fit for CSS-based responsive cards, graphs, icons, media art, and touch UI.
- Microsoft supports WebView2 in WPF and documents host/web communication patterns. See https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf

### Dashboard UI

Recommended stack:

- TypeScript
- Vite or another small bundler
- No React at first unless complexity justifies it
- CSS custom properties for theme tokens
- uPlot for small real-time graphs
- Lucide icons
- GridStack.js only when drag/resize layout editing becomes necessary

Initial UI should be deterministic and hand-laid out for 1920 x 550. Add drag-and-resize layout editing after the core widgets are reliable.

### Communication

Use WebView2 native-to-web messaging:

- C# host collects telemetry and app/device state.
- Host sends normalized JSON state to WebView2.
- UI sends user commands back to host: switch audio output, play/pause, seek, change volume, change display, save preferences.
- Do not expose a localhost HTTP server for MVP.

Example state message:

```json
{
  "type": "state:update",
  "payload": {
    "telemetry": {},
    "media": {},
    "audio": {},
    "display": {}
  }
}
```

## 6. Core Services

### Display Service

Responsibilities:

- Enumerate connected displays.
- Identify the ASUS PA147CDV when possible.
- Remember preferred display ID.
- Open dashboard as a borderless full-screen window on the target display.
- Recover to primary display when the target is missing.
- Provide a settings view for selecting a display manually.

Acceptance criteria:

- App opens full-screen on the ASUS at 1920 x 550.
- App does not get permanently lost if the ASUS is disconnected.
- User can reset display choice.

### Telemetry Service

Use LibreHardwareMonitor for system stats.

Reference: LibreHardwareMonitor supports monitoring temperatures, fan speeds, voltages, load, clocks, motherboard, CPU, GPU, storage, and network information; its library supports modern .NET targets. See https://github.com/LibreHardwareMonitor/LibreHardwareMonitor

License note:

- LibreHardwareMonitor is MPL 2.0.
- Keep it as an unmodified dependency where possible.
- Include third-party notices.
- If modifying MPL-covered files, keep those modifications under MPL 2.0.

Initial telemetry:

- CPU utilization
- CPU package temperature
- CPU package power if available
- GPU utilization
- GPU temperature
- GPU power if available
- VRAM usage if available
- RAM used/total
- Storage usage and NVMe temperature if available
- Network upload/download rate
- Fan and pump RPM where LibreHardwareMonitor exposes them

Known risk:

- ASUS motherboard sensors, Arctic pump RPM, and fan header names may vary.
- Some sensors may require administrator privileges.
- Sensor naming and availability must be tested on the actual PC.

### Media Session Service

Use Windows GlobalSystemMediaTransportControlsSessionManager.

Reference: Microsoft documents this API as providing access to system playback sessions that integrate with SystemMediaTransportControls, including playback info and remote control. It was introduced in Windows 10 version 1809. See https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager

MVP behavior:

- Prefer a currently playing Spotify session.
- If Spotify is not playing, prefer any currently playing session.
- If nothing is playing, retain the last controlled session.
- Show source app, title, artist, album, playback state, duration, and position.
- Show cover art from the Windows media session thumbnail.
- Provide play/pause, previous, next, and seek if supported.
- Animate progress locally between timeline updates.

Do not use Spotify Web API in MVP.

Why:

- Windows media sessions support Spotify without OAuth.
- Also works with supported browsers and other media apps.
- Avoids account setup, token storage, Spotify Premium restrictions, rate limits, and branding constraints.

Later optional Spotify extension:

- Spotify Connect device switching
- Remote speaker control
- Queue
- Shuffle/repeat
- Playlists
- Save to library

Treat that as a separate feature because it requires OAuth and user-provided Spotify developer configuration for open-source distribution.

### Global Audio Output Service

Scope:

- Global Windows output only.
- No per-application routing.

Use Windows Core Audio APIs:

- Enumerate playback endpoints.
- Read current default output.
- Subscribe to endpoint changes.
- Set global default output through a small isolated Windows compatibility layer.
- Control endpoint master volume and mute.
- Read peak levels for a visual meter.

References:

- Microsoft Core Audio device events: https://learn.microsoft.com/en-us/windows/win32/coreaudio/device-events
- Microsoft `IAudioEndpointVolume`: https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume

Implementation notes:

- Use NAudio for common WASAPI/Core Audio plumbing if it meaningfully reduces interop code. NAudio is MIT licensed.
- Microsoft documents enumeration, notifications, and volume APIs.
- Changing the system default output commonly uses the internal `IPolicyConfig` COM interface. Keep this behind a tiny isolated adapter so it can be replaced if Windows behavior changes.

MVP behavior:

- List active playback devices.
- Highlight current default.
- Let user switch default with one tap.
- Optionally set communications default at the same time.
- Show global volume slider and mute.
- Show live audio activity meter.
- Allow local friendly names.
- Allow hiding devices.
- Allow favorite output buttons.

## 7. MVP Widget Set

Build these first:

1. System Monitor
2. GPU Monitor
3. Media / Spotify-preferred Now Playing
4. Global Audio Output Selector

### System Monitor Widget

Display:

- CPU usage
- CPU temperature
- CPU power if available
- RAM usage
- Network up/down
- Mini history graph

### GPU Monitor Widget

Display:

- GPU usage
- GPU temperature
- GPU power if available
- VRAM usage
- Mini history graph

### Media Widget

Display:

- Large cover art
- Track title
- Artist
- Album when available
- App source, preferring Spotify
- Play/pause
- Previous/next
- Timeline/progress bar
- Touch seeking when supported

Design notes:

- Use large touch targets.
- Use a local blurred artwork backdrop only as a visual treatment.
- Keep original cover art intact in the main square.
- Handle missing or delayed artwork without freezing the UI.

### Audio Output Widget

Display:

- 4 to 6 favorite output buttons
- Current output checkmark
- All outputs drawer/menu
- Volume slider
- Mute button
- Optional "also set communications device" toggle
- Live level meter

## 8. UI Design Direction

Canvas:

- Primary fixed target: 1920 x 550.
- Must remain readable at 100% Windows scaling.
- Later support: compact ultrawide, 2560 x 720, portrait strips.

Visual style:

- Dark dashboard.
- High contrast.
- Cyan/blue accent allowed, but avoid copying Corsair branding.
- Cards with modest radius, not huge pill shapes.
- Use real data density. No marketing hero page.
- Use icons in controls, with tooltips in settings/edit modes.
- Keep text short and scannable.
- Use stable widget dimensions to prevent layout shifts.

Touch behavior:

- Minimum practical touch target: 44 x 44 CSS pixels.
- Avoid accidental drags when tapping controls.
- Make progress bars and sliders easy to touch.
- Add pressed/active states.

## 9. Proposed Repository Structure

```text
OpenPanel/
  README.md
  LICENSE
  NOTICE.md
  docs/
    product-brief.md
    architecture.md
    hardware-notes.md
    telemetry-sensors.md
    audio-output.md
    media-widget.md
    testing.md
  src/
    OpenPanel.sln
    OpenPanel.Host/
      OpenPanel.Host.csproj
      App.xaml
      App.xaml.cs
      MainWindow.xaml
      MainWindow.xaml.cs
      Services/
        DisplayService.cs
        TelemetryService.cs
        MediaSessionService.cs
        AudioDeviceService.cs
        SettingsService.cs
      Interop/
        AudioPolicyConfig/
      Models/
      Messaging/
      Resources/
    OpenPanel.Host.Tests/
    OpenPanel.Ui/
      package.json
      vite.config.ts
      index.html
      src/
        main.ts
        app.ts
        bridge.ts
        state/
        widgets/
          system/
          gpu/
          media/
          audio-output/
        styles/
          tokens.css
          layout.css
          widgets.css
  scripts/
    setup.ps1
    build.ps1
    run.ps1
    test.ps1
    package.ps1
  samples/
    telemetry-snapshots/
    media-session-snapshots/
```

## 10. Coding Standards

C#:

- Enable nullable reference types.
- Use async APIs where appropriate.
- Keep Windows interop isolated in small classes.
- Keep services testable through interfaces.
- Avoid leaking raw platform objects into UI message models.
- Use structured logging.
- Treat hardware/API failures as normal runtime conditions.

TypeScript:

- Strict mode.
- Central state model.
- No direct dependency on C# object shapes outside a typed bridge.
- Components should render from state and send commands through the bridge.
- Keep CSS tokens centralized.

General:

- Commit in small milestones.
- Keep README usable by a non-expert Windows user.
- Include third-party license notices early.
- Do not add a heavy framework unless a real complexity threshold is crossed.

## 11. Testing Strategy

Unit tests:

- State normalization for telemetry.
- Media session selection rules.
- Audio device sorting, favorites, hide/rename behavior.
- Display selection persistence and fallback.
- JSON bridge message parsing.

Integration/manual tests:

- Run on Windows with ASUS PA147CDV attached.
- Verify full-screen positioning.
- Disconnect/reconnect ASUS and confirm recovery.
- Start/stop Spotify and confirm media widget updates.
- Change tracks and confirm artwork updates.
- Switch between headphones, desk speakers, HDMI, Bluetooth, and virtual devices.
- Confirm global volume/mute changes Windows state.
- Confirm no crash when LibreHardwareMonitor cannot read a sensor.

Visual tests:

- Capture screenshots at 1920 x 550.
- Check text clipping.
- Check that buttons remain touch-sized.
- Check missing-data states.
- Check high/low values in widgets.

Performance:

- Telemetry refresh should not visibly stutter the UI.
- UI should update smoothly without excessive CPU use.
- Avoid pushing large artwork blobs repeatedly.

## 12. Acceptance Criteria for First Usable MVP

The MVP is acceptable when:

- App launches on Windows.
- User can choose the ASUS PA147CDV as the dashboard display.
- Dashboard runs borderless full-screen at 1920 x 550.
- CPU, GPU, RAM, and network widgets display live values.
- Spotify playback appears in the media widget when Spotify is active.
- Media controls work: play/pause, next, previous, and progress display.
- Active Windows playback session fallback works for non-Spotify apps.
- Audio output widget lists active output devices.
- One tap changes the global default output.
- Volume and mute work.
- Device rename/hide/favorite settings persist.
- App recovers if a sensor, media session, or audio endpoint disappears.
- README explains setup, requirements, and known limitations.
- Third-party license notices are present.

## 13. Risks and Mitigations

### Sensor Availability

Risk: Some ASUS motherboard sensors, fan headers, or pump RPMs may not be exposed reliably.

Mitigation:

- Start with CPU/GPU/RAM/network.
- Log raw sensor names.
- Add a sensor-mapping screen later.
- Document that some sensors require administrator privileges.

### Windows Default Audio Output API

Risk: Setting the global default output uses a widely used but not fully public interface.

Mitigation:

- Isolate implementation behind `IAudioDefaultDeviceSwitcher`.
- Add a fallback button to open Windows Sound settings.
- Keep switching limited to global output, not per-app routing.

### Display Renumbering

Risk: Windows can change display IDs after driver updates, port changes, or GPU changes.

Mitigation:

- Store preferred display ID plus resolution/name hints.
- Provide recovery mode on primary display.
- Provide reset display selection command.

### WebView2 Runtime

Risk: Missing or old WebView2 runtime could cause blank UI.

Mitigation:

- Detect runtime at startup.
- Link to Microsoft WebView2 Evergreen runtime.
- Later bundle or install runtime through the installer if needed.

### Scope Creep

Risk: Trying to match iCUE, Stream Deck, AIDA64, Spotify Connect, and EarTrumpet all at once.

Mitigation:

- First release only includes telemetry, media, and global audio output.
- No plugin system in MVP.
- No per-application audio routing in MVP.
- No Spotify Web API in MVP.

## 14. Licensing Plan

Recommended project license:

- MIT for original OpenPanel code.

Likely dependencies:

- LibreHardwareMonitor: MPL 2.0
- NAudio: MIT
- uPlot: MIT
- GridStack.js: MIT, if used
- Lucide: ISC
- WebView2 SDK: Microsoft package terms

Required files:

- `LICENSE` for OpenPanel.
- `NOTICE.md` with third-party dependencies and licenses.
- Keep dependency license files or links available.

Branding:

- Do not use Corsair, XENEON, iCUE, ASUS, Spotify, or Microsoft logos unless documentation/legal guidance clearly allows it.
- Text can say "Spotify" only as a supported media app/source where appropriate.
- Avoid implying endorsement by any vendor.

## 15. Development Environment Recommendation

### Short Answer

Use **native Windows Codex / ChatGPT desktop app in Codex mode**, not WSL, for this project.

Use WSL only as an optional helper environment for Linux-style tools. Do not make WSL the primary Codex workspace for the Windows app.

### Why Native Windows Is Better Here

This project must directly build, run, and test Windows desktop behavior:

- WPF or WinUI desktop windowing
- WebView2 runtime
- Windows display enumeration
- Windows touch input
- Windows media sessions
- Windows Core Audio endpoints
- Global default audio output switching
- System tray behavior
- Windows startup integration
- LibreHardwareMonitor access to local Windows hardware sensors

Codex is available in the ChatGPT desktop app on macOS and Windows, and OpenAI describes Codex as the experience for writing/debugging code, running tests and commands, reviewing changes, and working with local folders, repositories, terminals, and developer tools. See OpenAI Help: https://help.openai.com/en/articles/20001275-chatgpt-work-and-codex

OpenAI also says the new ChatGPT desktop app includes Codex on macOS and Windows, with Codex kept as a separate view and local workflows unchanged. See https://help.openai.com/en/articles/20001276-moving-to-the-new-chatgpt-desktop-app

For this project, the local Windows machine is not just a build machine. It is the target device.

### Why WSL Is Not the Primary Choice

Microsoft describes WSL as a way to run Linux distributions and Linux command-line tools on Windows. It is excellent for Linux-first development, web backends, shell tools, and cross-platform projects. See https://learn.microsoft.com/en-us/windows/wsl/about

However, Microsoft also recommends storing files on the same operating system as the tools you plan to use. If using Windows tools, store files in the Windows file system; if using Linux tools, store files in the WSL file system. Accessing files across operating systems can slow performance. See https://learn.microsoft.com/en-us/windows/wsl/setup/environment

Microsoft's WSL comparison also notes that WSL 2 is the default and has strong Linux compatibility, but performance across OS file systems is an exception; the guidance is to keep project files with the same OS as the tools. See https://learn.microsoft.com/en-us/windows/wsl/compare-versions

Because this app relies on Windows desktop APIs, Windows SDK tooling, .NET desktop workloads, audio devices, media sessions, and physical display/touch behavior, keeping the repo in native Windows is simpler.

### Recommended PC Setup

Create the repository on the Windows file system:

```text
C:\Dev\OpenPanel
```

Install:

- ChatGPT desktop app with Codex mode
- Git for Windows
- .NET 10 SDK
- Visual Studio 2026 or Visual Studio 2022 with .NET desktop development workload, depending on current availability and compatibility
- WebView2 Evergreen Runtime if not already installed
- Node.js LTS for the UI build
- Optional: HWiNFO for comparison/debugging, not for MVP dependency

Open `C:\Dev\OpenPanel` in Codex on the Windows PC.

Do not place the main repo under:

```text
\\wsl$\Ubuntu\home\...
```

Do not make the main repo:

```text
/mnt/c/...
```

from inside a WSL-first workflow.

### When WSL Is Still Useful

WSL can still help with:

- Running Linux shell utilities.
- Comparing open-source project behavior.
- Lightweight scripts that do not touch Windows APIs.
- Documentation generation.

But those should be optional helpers. The application should build and run from Windows tooling.

## 16. First Codex Task List

Give Codex this initial task after creating the empty repository:

1. Create the repository skeleton shown in this handoff.
2. Create `README.md`, `docs/product-brief.md`, `docs/architecture.md`, and `NOTICE.md`.
3. Create a .NET 10 WPF host project named `OpenPanel.Host`.
4. Add a basic WebView2-hosted dashboard window.
5. Make the app open a borderless 1920 x 550 window on a selected display.
6. Add a placeholder dashboard UI with four widgets: System, GPU, Media, Audio Output.
7. Add a typed message bridge between C# and TypeScript.
8. Add stub services for Display, Telemetry, Media Session, Audio Device, and Settings.
9. Add `scripts/setup.ps1`, `scripts/build.ps1`, `scripts/run.ps1`, and `scripts/test.ps1`.
10. Add a first verification checklist for running on the ASUS PA147CDV.

Do not implement hardware telemetry, audio switching, or media controls in the very first commit. First establish the shell, dashboard rendering, bridge, repo structure, and run scripts.

## 17. Suggested First Prompt to Codex

Paste this into the new Codex task:

```text
We are starting OpenPanel, a Windows-first open-source touchscreen dashboard for the ASUS ProArt PA147CDV at 1920 x 550.

Use the attached project handoff as the source of truth. For the first milestone, do not implement full hardware telemetry, media controls, or audio switching yet. Create the repository skeleton, a .NET 10 WPF host, a WebView2 dashboard surface, a TypeScript UI project, placeholder widgets, typed host/UI messaging, run/build/test scripts, and initial docs.

Keep the app Windows-native. Assume the repo lives at C:\Dev\OpenPanel. Use native Windows tooling rather than WSL. Keep dependencies minimal and document every dependency added. The first acceptance target is: the app launches, opens a borderless 1920 x 550 dashboard window, renders four placeholder widgets, and the C# host can send a sample state update to the UI.
```

## 18. Source Links Used for This Handoff

OpenAI:

- ChatGPT Work and Codex: https://help.openai.com/en/articles/20001275-chatgpt-work-and-codex
- Moving to the new ChatGPT desktop app: https://help.openai.com/en/articles/20001276-moving-to-the-new-chatgpt-desktop-app
- Using Codex with your ChatGPT plan: https://help.openai.com/en/articles/11369540-using-codex-with-chatgpt

Microsoft:

- WSL overview: https://learn.microsoft.com/en-us/windows/wsl/about
- WSL development environment and file storage guidance: https://learn.microsoft.com/en-us/windows/wsl/setup/environment
- WSL 1 vs WSL 2 comparison: https://learn.microsoft.com/en-us/windows/wsl/compare-versions
- .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy
- WebView2 in WPF: https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf
- Global media sessions: https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager
- Core Audio device events: https://learn.microsoft.com/en-us/windows/win32/coreaudio/device-events
- Endpoint volume API: https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume

Hardware and open-source components:

- ASUS PA147CDV official specs: https://www.asus.com/us/displays-desktops/monitors/proart/proart-display-pa147cdv/techspec/
- Corsair XENEON EDGE official page: https://www.corsair.com/us/en/p/monitors/cc-9011306-ww/xeneon-edge-14-5-lcd-touchscreen-cc-9011306-ww
- LibreHardwareMonitor: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
