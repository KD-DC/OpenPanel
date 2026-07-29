# Telemetry Sensors

Telemetry is not implemented in the first milestone.

Planned source:

- LibreHardwareMonitor as an unmodified dependency where possible.

Initial future signals:

- CPU utilization.
- CPU package temperature.
- CPU package power when available.
- GPU utilization.
- GPU temperature.
- GPU power when available.
- VRAM usage.
- RAM used and total.
- Storage usage and NVMe temperature when available.
- Network upload and download rates.

Hardware sensor failures should be treated as normal runtime conditions. Some sensors may require administrator privileges or may not be exposed on a given PC.
