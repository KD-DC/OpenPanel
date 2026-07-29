using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public interface IMediaSessionService
{
    Task<MediaSummary> GetCurrentSessionAsync(CancellationToken cancellationToken);
}

public sealed class MediaSessionService : IMediaSessionService
{
    public Task<MediaSummary> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        var session = new MediaSummary("Unavailable", "No active session", string.Empty, string.Empty, false, 0, 0);
        return Task.FromResult(session);
    }
}
