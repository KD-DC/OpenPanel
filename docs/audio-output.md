# Global Audio Output

OpenPanel controls the global Windows playback output only. It does not perform per-application audio routing.

## Behavior

- Enumerates active render endpoints through NAudio/Core Audio.
- Highlights the current multimedia default endpoint.
- Sets console and multimedia defaults with one tap.
- Optionally sets the communications default at the same time.
- Reads and changes endpoint master volume and mute.
- Reads the current endpoint peak level for the activity meter.
- Treats endpoint removal and temporary Core Audio failures as normal unavailable states.

Microsoft documents endpoint enumeration, notifications, volume, mute, and metering. Windows does not expose a fully public default-endpoint setter, so `Interop/AudioPolicyConfig/AudioDefaultDeviceSwitcher.cs` contains the compatibility layer. Keep that adapter isolated and covered by manual Windows integration testing.

OpenPanel writes command-only diagnostics to `%LOCALAPPDATA%\OpenPanel\openpanel.log`. The polling loop does not write log entries.
