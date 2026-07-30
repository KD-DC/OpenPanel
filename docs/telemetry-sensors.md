# Telemetry Sensors

OpenPanel samples live read-only CPU, GPU, memory, and network telemetry once per second. Storage sensors are sampled every five seconds because drive health and capacity values change slowly and SMART access can be comparatively expensive.

## Sources

- LibreHardwareMonitorLib 0.9.6: CPU/GPU utilization, clock, power, fan, temperature, VRAM, memory, and storage sensors.
- `GlobalMemoryStatusEx`: reliable physical and committed-memory used, available, and total values.
- `DriveInfo`: fixed-volume used percentage when LibreHardwareMonitor cannot expose storage devices without elevation.
- .NET network-interface statistics: upload and download byte deltas.
- `System.Diagnostics.Process`: on-demand per-application CPU deltas and
  working-set memory while the Hardware detail view is open.

Only the LibreHardwareMonitor CPU, GPU, memory, and storage categories are enabled. Motherboard, controller, battery, PSU, network, and power-monitor groups remain disabled to limit initialization work and avoid duplicating the existing network sampler.

Network rates use the busiest active non-loopback interface in each direction. This avoids double-counting traffic that appears on both a VPN or virtual adapter and its physical interface.

## Runtime Behavior

- Polling is serialized; snapshots never overlap.
- Hardware work runs away from the WPF UI thread.
- The first network sample is zero because a byte delta requires two observations.
- Storage values are cached between five-second updates.
- Application CPU and memory rankings sample every two seconds only while their
  expanded view is open; collapsed Hardware performs no process enumeration.
- A one-time storage sensor inventory is written to `%LOCALAPPDATA%\OpenPanel\openpanel.log` for hardware diagnostics.
- Hardware initialization retries no more than once every 30 seconds after failure.
- Polling is canceled and LibreHardwareMonitor is closed when the dashboard window closes.

Sensor failures are normal runtime conditions. Temperature, VRAM, and advanced storage values may be unavailable depending on hardware, drivers, permissions, and vendor API support. OpenPanel runs as the current user and does not request administrator elevation. Fixed-volume capacity remains available through the native fallback.

## Current Signals

- CPU utilization, package temperature, average core clock, and package power.
- GPU utilization, temperatures, clocks, VRAM, power, fan speed, and fan control.
- Physical RAM used, available, total, and load.
- Committed virtual memory used and limit.
- Network upload and download rate.
- Storage used percentage, total activity, temperature, and read/write rates when exposed.

Unsupported sensor values are shown as `--`.
