using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class DashboardStateProvider
{
    public DashboardState CreateState(
        HardwareTelemetrySnapshot telemetry,
        DisplaySummary display)
    {
        return new DashboardState(
            telemetry.Telemetry,
            telemetry.Gpu,
            new MediaSummary(
                Source: "Not connected",
                Title: "Media controls are not implemented",
                Artist: "OpenPanel",
                Album: string.Empty,
                IsPlaying: false,
                PositionSeconds: 0,
                DurationSeconds: 0),
            new AudioSummary(
                CurrentOutput: "Not connected",
                VolumePercent: 0,
                IsMuted: false,
                Outputs: Array.Empty<AudioOutputSummary>()),
            display);
    }
}
