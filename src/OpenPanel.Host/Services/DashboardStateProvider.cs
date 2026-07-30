using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class DashboardStateProvider
{
    public DashboardState CreateState(
        HardwareTelemetrySnapshot telemetry,
        NetworkQualitySummary network,
        PeripheralBatterySummary peripherals,
        GamingPerformanceSummary gaming,
        MediaSummary media,
        AudioSummary audio,
        WeatherSummary weather,
        string appearance,
        DisplaySummary display)
    {
        return new DashboardState(
            telemetry.Telemetry,
            telemetry.Gpu,
            telemetry.Advanced,
            telemetry.Storage,
            network,
            peripherals,
            gaming,
            media,
            audio,
            weather,
            new AppearanceSummary(appearance),
            display);
    }
}
