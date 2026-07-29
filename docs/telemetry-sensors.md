# Telemetry Sensors

OpenPanel samples live read-only CPU, GPU, memory, and network telemetry once per second. Motherboard, Super-I/O, and embedded-controller sensors are sampled every three seconds because those values change more slowly and some firmware interfaces are comparatively expensive.

## Sources

- LibreHardwareMonitorLib 0.9.6: CPU/GPU utilization, clock, power, fan, temperature, VRAM, and memory sensors.
- `GlobalMemoryStatusEx`: reliable physical and committed-memory used, available, and total values.
- .NET network-interface statistics: upload and download byte deltas.

Only the LibreHardwareMonitor CPU, GPU, and memory categories are enabled. Motherboard, storage, controller, battery, and other sensor groups remain disabled to limit initialization work and polling overhead.

Network rates use the busiest active non-loopback interface in each direction. This avoids double-counting traffic that appears on both a VPN/virtual adapter and its physical interface.

## Runtime Behavior

- Polling is serialized; snapshots never overlap.
- Hardware work runs away from the WPF UI thread.
- The first network sample is zero because a byte delta requires two observations.
- Hardware initialization retries no more than once every 30 seconds after failure.
- Polling is canceled and LibreHardwareMonitor is closed when the dashboard window closes.

Sensor failures are normal runtime conditions. Temperature or VRAM values may be unavailable depending on hardware, drivers, permissions, and vendor API support. OpenPanel runs as the current user and does not request administrator elevation.

## Motherboard sensors

The motherboard page enables LibreHardwareMonitor motherboard support and displays validated readings in four groups:

- Temperatures, such as VRM, chipset, motherboard, CPU socket, external probes, and water temperature.
- Cooling, including CPU, chassis, pump, and AIO fan speeds.
- Voltage rails, including 12 V, 5 V, 3.3 V, CPU core, SoC, and DRAM when exposed.
- Board-level power readings when supported.

Sensor names and availability are determined by the board firmware and LibreHardwareMonitor. OpenPanel filters implausible readings and shows an unavailable state rather than inventing a value. At hardware initialization it writes one local motherboard sensor inventory to `%LOCALAPPDATA%\OpenPanel\openpanel.log` to make board-specific mapping issues diagnosable without continuous telemetry logging.

LibreHardwareMonitor 0.9.6 does not currently expose motherboard sensors on the ASUS ROG STRIX X870E-E GAMING WIFI. OpenPanel still includes the typed provider and widget surface so supported boards work automatically and future LibreHardwareMonitor board support can flow through without an ASUS-specific protocol.

## Current Signals

- CPU utilization.
- CPU package/die temperature when available.
- CPU average core clock and package power when available.
- GPU utilization.
- GPU temperature when available.
- GPU core/memory clocks, board power, fan speed/control, hot-spot temperature, and memory-junction temperature when available.
- GPU memory used and total when available.
- Physical RAM used, available, total, and load.
- Committed virtual memory used and limit.
- Network upload and download rate.

Storage sensors are not enabled yet. Unsupported sensor values are shown as `--`.
