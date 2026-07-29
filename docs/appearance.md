# Appearance

OpenPanel defaults to the `Media OLED` first-page appearance. The previous first-page layout remains available temporarily for real-device comparison.

## Selecting an Appearance

1. Right-click the OpenPanel system tray icon.
2. Open `Appearance`.
3. Select `Current` or `Media OLED`.

The dashboard updates on the next one-second state snapshot. The second telemetry page is shared by both appearances.

## Persistence

The host stores the selected value in:

```text
%LOCALAPPDATA%\OpenPanel\settings.json
```

The file is written only when the appearance changes. Missing, unreadable, or unsupported settings fall back to `Media OLED`.

The appearance value is included in the typed host-to-UI dashboard state. The UI rebuilds the page shell only when that value changes; normal telemetry updates continue to update individual widget slots.

No dependency was added for appearance persistence or rendering.
