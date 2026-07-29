namespace OpenPanel.Host.Services;

public interface ISettingsService
{
    Task SaveAsync(CancellationToken cancellationToken);
}

public sealed class SettingsService : ISettingsService
{
    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
