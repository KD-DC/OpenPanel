# Testing

## Automated Checks

Run:

```powershell
.\scripts\test.ps1
```

This type-checks the TypeScript UI and runs .NET tests.

## First Hardware Checklist

- Install .NET 10 SDK, Node.js LTS, and WebView2 Evergreen Runtime.
- Connect the ASUS PA147CDV to Windows.
- Confirm Windows reports the display at 1920 x 550.
- Run `.\scripts\setup.ps1`.
- Run `.\scripts\build.ps1`.
- Run `.\scripts\run.ps1`.
- Confirm the OpenPanel window appears borderless on the ASUS display.
- Confirm OpenPanel has no taskbar button, has a system tray icon, and the tray Open and Exit actions work.
- Connect or disconnect a Bluetooth playback device and confirm the audio output list updates without restarting OpenPanel.
- Swipe to the power-user page and confirm detected storage devices render or display the explicit unavailable state.
- Confirm four widgets render: System, GPU, Media, Audio Output.
- Confirm CPU, GPU, and RAM values are nonzero when the machine is active.
- Create brief CPU or GPU activity and confirm utilization changes within two seconds.
- Transfer data and confirm network upload/download values change after the first sample.
- Read or write a file and confirm storage activity and transfer rates update within five seconds.
- Confirm unavailable temperature or VRAM sensors render as `--` or zero without crashing.
- Disconnect the ASUS and relaunch; confirm the window appears on the primary display.
