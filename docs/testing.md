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
- Confirm four widgets render: System, GPU, Media, Audio Output.
- Confirm placeholder values update from the host sample state after load.
- Disconnect the ASUS and relaunch; confirm the window appears on the primary display.
