using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class DashboardStateProvider
{
    public DashboardState CreateState(
        HardwareTelemetrySnapshot telemetry,
        NetworkQualitySummary network,
        ProcessUsageSummary processes,
        PeripheralBatterySummary peripherals,
        GamingPerformanceSummary gaming,
        MediaSummary media,
        AudioSummary audio,
        WeatherSummary weather,
        string appearance,
        WidgetConfigurationSummary widgets,
        DisplaySummary display)
    {
        return new DashboardState(
            telemetry.Telemetry,
            telemetry.Gpu,
            telemetry.Advanced,
            telemetry.Storage,
            network,
            processes,
            peripherals,
            gaming,
            media,
            audio,
            weather,
            new AppearanceSummary(appearance),
            widgets,
            display);
    }
}
