using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public interface IAudioDeviceService
{
    Task<AudioSummary> GetOutputsAsync(CancellationToken cancellationToken);
}

public sealed class AudioDeviceService : IAudioDeviceService
{
    public Task<AudioSummary> GetOutputsAsync(CancellationToken cancellationToken)
    {
        var summary = new AudioSummary("Unavailable", 0, false, Array.Empty<AudioOutputSummary>());
        return Task.FromResult(summary);
    }
}
