using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class DashboardStateProvider
{
    public DashboardState CreateState(
        HardwareTelemetrySnapshot telemetry,
        MediaSummary media,
        AudioSummary audio,
        string appearance,
        DisplaySummary display)
    {
        return new DashboardState(
            telemetry.Telemetry,
            telemetry.Gpu,
            telemetry.Advanced,
            telemetry.Storage,
            media,
            audio,
            new AppearanceSummary(appearance),
            display);
    }
}
