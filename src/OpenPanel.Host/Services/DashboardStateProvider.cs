using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class DashboardStateProvider
{
    public DashboardState CreateSampleState()
    {
        return new DashboardState(
            new TelemetrySummary(
                CpuUsagePercent: 18,
                CpuTemperatureCelsius: 43,
                MemoryUsedGb: 21.4,
                MemoryTotalGb: 64,
                NetworkUploadMbps: 4.2,
                NetworkDownloadMbps: 82.5),
            new GpuSummary(
                GpuUsagePercent: 36,
                GpuTemperatureCelsius: 51,
                VramUsedGb: 6.8,
                VramTotalGb: 16),
            new MediaSummary(
                Source: "Sample session",
                Title: "Waiting for media service",
                Artist: "OpenPanel",
                Album: "Placeholder",
                IsPlaying: false,
                PositionSeconds: 42,
                DurationSeconds: 210),
            new AudioSummary(
                CurrentOutput: "Desk Speakers",
                VolumePercent: 72,
                IsMuted: false,
                Outputs:
                [
                    new AudioOutputSummary("speakers", "Desk Speakers", true),
                    new AudioOutputSummary("headphones", "Headphones", false),
                    new AudioOutputSummary("hdmi", "HDMI Display", false)
                ]),
            new DisplaySummary(
                Name: "ASUS PA147CDV target",
                Left: 0,
                Top: 0,
                Width: 1920,
                Height: 550,
                IsPrimary: false));
    }
}
