# Telemetry Sensors

OpenPanel samples live read-only telemetry once per second.

## Sources

- LibreHardwareMonitorLib 0.9.6: CPU/GPU utilization, temperature, and VRAM sensors.
- `GlobalMemoryStatusEx`: physical RAM used and total.
- .NET network-interface statistics: upload and download byte deltas.

Only the LibreHardwareMonitor CPU and GPU categories are enabled. Motherboard, storage, controller, battery, and other sensor groups remain disabled to limit initialization work and polling overhead.

Network rates use the busiest active non-loopback interface in each direction. This avoids double-counting traffic that appears on both a VPN/virtual adapter and its physical interface.

## Runtime Behavior

- Polling is serialized; snapshots never overlap.
- Hardware work runs away from the WPF UI thread.
- The first network sample is zero because a byte delta requires two observations.
- Hardware initialization retries no more than once every 30 seconds after failure.
- Polling is canceled and LibreHardwareMonitor is closed when the dashboard window closes.

Sensor failures are normal runtime conditions. Temperature or VRAM values may be unavailable depending on hardware, drivers, permissions, and vendor API support. OpenPanel runs as the current user and does not request administrator elevation.

## Current Signals

- CPU utilization.
- CPU package/die temperature when available.
- GPU utilization.
- GPU temperature when available.
- GPU memory used and total when available.
- RAM used and total.
- Network upload and download rate.

Storage, fan, and power signals are not enabled yet.
