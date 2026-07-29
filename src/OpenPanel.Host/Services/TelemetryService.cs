using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public interface ITelemetryService
{
    Task<TelemetrySummary> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class TelemetryService : ITelemetryService
{
    public Task<TelemetrySummary> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshot = new TelemetrySummary(0, null, 0, 0, 0, 0);
        return Task.FromResult(snapshot);
    }
}
