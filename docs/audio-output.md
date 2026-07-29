# Audio Control Center

OpenPanel keeps global playback selection and volume in a compact widget. An
expanded mode adds capture-device controls and per-application volume without
changing application routing.

## Behavior

- Enumerates active render endpoints through NAudio/Core Audio.
- Re-enumerates endpoints during each state refresh so newly connected Bluetooth and USB outputs appear without restarting OpenPanel.
- Keeps output buttons in stable name order when the default endpoint changes, so selecting an output never moves its button.
- Highlights the current multimedia default endpoint.
- Sets console and multimedia defaults with one tap.
- Optionally sets the communications default at the same time.
- Reads and changes endpoint master volume and mute.
- Preserves each endpoint's Windows volume and mute state when switching, avoiding an unexpected loud output. The active endpoint button shows `MUTE` when that endpoint needs to be unmuted.
- Reads the current endpoint peak level for the activity meter.
- Expands in place without moving or reordering the output buttons.
- Enumerates active capture endpoints and controls the default microphone volume
  and mute state while expanded.
- Groups active render sessions by process and exposes volume, mute, and peak
  activity for up to five applications on the current output.
- Starts capture and application-session discovery only while the expanded panel
  is open.
- Treats endpoint removal and temporary Core Audio failures as normal unavailable states.
- Continues refreshing the output list after a volume slider or communications checkbox receives focus; only an active press or drag defers replacement.

Microsoft documents endpoint enumeration, audio sessions, volume, mute, and
metering. Windows does not expose a fully public default-endpoint setter, so
`Interop/AudioPolicyConfig/AudioDefaultDeviceSwitcher.cs` contains the
compatibility layer for playback and capture defaults. Keep that adapter
isolated and covered by manual Windows integration testing.

OpenPanel writes command diagnostics and a one-time storage sensor inventory to `%LOCALAPPDATA%\OpenPanel\openpanel.log`. Polling loops do not write continuous log entries.
