using System.IO;
using Windows.Media.Control;
using Windows.Storage.Streams;
using OpenPanel.Host.Interop.MediaKeys;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public interface IMediaSessionService
{
    Task<MediaSummary> GetCurrentSessionAsync(CancellationToken cancellationToken);
    Task TogglePlayPauseAsync(CancellationToken cancellationToken);
    Task GoPreviousAsync(CancellationToken cancellationToken);
    Task GoNextAsync(CancellationToken cancellationToken);
    Task SetShuffleAsync(bool isActive, CancellationToken cancellationToken);
    Task SeekAsync(double positionSeconds, CancellationToken cancellationToken);
}

public sealed class MediaSessionService : IMediaSessionService
{
    private readonly SemaphoreSlim gate = new(1, 1);

    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private string? lastSourceAppId;
    private string? artworkKey;
    private string? artworkDataUrl;
    private DateTimeOffset lastArtworkSentAt;

    public async Task<MediaSummary> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var session = await SelectSessionAsync(cancellationToken);
            return session is null
                ? EmptyState()
                : await ReadSessionAsync(session, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return EmptyState();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task TogglePlayPauseAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var session = await SelectSessionAsync(cancellationToken);
            if (session is null)
            {
                return;
            }

            if (IsSpotify(session.SourceAppUserModelId))
            {
                MediaKeySender.PlayPause();
                lastSourceAppId = session.SourceAppUserModelId;
                return;
            }

            var playbackStatus = session.GetPlaybackInfo().PlaybackStatus;
            var accepted = playbackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
                    ? await session.TryPauseAsync()
                    : await session.TryPlayAsync();
            if (!accepted)
            {
                accepted = await session.TryTogglePlayPauseAsync();
            }
            if (!accepted)
            {
                throw new InvalidOperationException(
                    $"Media session '{session.SourceAppUserModelId}' rejected play/pause.");
            }

            lastSourceAppId = session.SourceAppUserModelId;
        }
        finally
        {
            gate.Release();
        }
    }

    public Task GoPreviousAsync(CancellationToken cancellationToken)
    {
        return RunCommandAsync(
            session => session.TrySkipPreviousAsync(),
            cancellationToken,
            MediaKeySender.Previous);
    }

    public Task GoNextAsync(CancellationToken cancellationToken)
    {
        return RunCommandAsync(
            session => session.TrySkipNextAsync(),
            cancellationToken,
            MediaKeySender.Next);
    }

    public Task SetShuffleAsync(bool isActive, CancellationToken cancellationToken)
    {
        return RunCommandAsync(
            session => session.TryChangeShuffleActiveAsync(isActive),
            cancellationToken);
    }

    public Task SeekAsync(double positionSeconds, CancellationToken cancellationToken)
    {
        var ticks = (long)Math.Max(0, positionSeconds * TimeSpan.TicksPerSecond);
        return RunCommandAsync(
            session => session.TryChangePlaybackPositionAsync(ticks),
            cancellationToken);
    }

    private async Task RunCommandAsync(
        Func<GlobalSystemMediaTransportControlsSession, Windows.Foundation.IAsyncOperation<bool>> command,
        CancellationToken cancellationToken,
        Action? spotifyCommand = null)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var session = await SelectSessionAsync(cancellationToken);
            if (session is not null)
            {
                if (spotifyCommand is not null &&
                    IsSpotify(session.SourceAppUserModelId))
                {
                    spotifyCommand();
                    lastSourceAppId = session.SourceAppUserModelId;
                    return;
                }

                var accepted = await command(session);
                if (!accepted)
                {
                    throw new InvalidOperationException(
                        $"Media session '{session.SourceAppUserModelId}' rejected the command.");
                }
                lastSourceAppId = session.SourceAppUserModelId;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<GlobalSystemMediaTransportControlsSession?> SelectSessionAsync(
        CancellationToken cancellationToken)
    {
        manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var sessions = manager.GetSessions().ToArray();
        var currentSourceAppId = manager.GetCurrentSession()?.SourceAppUserModelId;
        var candidates = sessions
            .Select(session => new MediaSessionCandidate(
                session.SourceAppUserModelId,
                IsPlaying(session)))
            .ToArray();
        var selectedIndex = MediaSessionSelector.SelectIndex(
            candidates,
            lastSourceAppId,
            currentSourceAppId);
        var selected = selectedIndex >= 0 ? sessions[selectedIndex] : null;

        if (selected is not null)
        {
            lastSourceAppId = selected.SourceAppUserModelId;
        }

        return selected;
    }

    private async Task<MediaSummary> ReadSessionAsync(
        GlobalSystemMediaTransportControlsSession session,
        CancellationToken cancellationToken)
    {
        var properties = await session.TryGetMediaPropertiesAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var playback = session.GetPlaybackInfo();
        var controls = playback.Controls;
        var timeline = session.GetTimelineProperties();
        var duration = Math.Max(0, (timeline.EndTime - timeline.StartTime).TotalSeconds);
        var position = Math.Clamp(
            (timeline.Position - timeline.StartTime).TotalSeconds,
            0,
            duration > 0 ? duration : double.MaxValue);
        var source = FormatSource(session.SourceAppUserModelId);
        var nextArtworkKey = string.Join(
            '\u001f',
            session.SourceAppUserModelId,
            properties.Title,
            properties.Artist,
            properties.AlbumTitle);

        var artworkChanged = !string.Equals(
            nextArtworkKey,
            artworkKey,
            StringComparison.Ordinal);
        if (artworkChanged)
        {
            artworkDataUrl = await ReadArtworkDataUrlAsync(properties.Thumbnail, cancellationToken);
            artworkKey = nextArtworkKey;
        }
        var shouldSendArtwork =
            artworkChanged ||
            DateTimeOffset.UtcNow - lastArtworkSentAt >= TimeSpan.FromSeconds(30);
        if (shouldSendArtwork)
        {
            lastArtworkSentAt = DateTimeOffset.UtcNow;
        }

        return new MediaSummary(
            source,
            string.IsNullOrWhiteSpace(properties.Title) ? "No active media" : properties.Title,
            properties.Artist ?? string.Empty,
            properties.AlbumTitle ?? string.Empty,
            properties.AlbumArtist ?? string.Empty,
            properties.Subtitle ?? string.Empty,
            properties.Genres?.ToArray() ?? [],
            properties.TrackNumber,
            properties.AlbumTrackCount,
            playback.PlaybackStatus.ToString(),
            properties.PlaybackType?.ToString() ?? string.Empty,
            playback.IsShuffleActive,
            playback.AutoRepeatMode?.ToString() ?? string.Empty,
            playback.PlaybackRate,
            shouldSendArtwork ? artworkDataUrl : null,
            playback.PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
            position,
            duration,
            controls.IsPlayPauseToggleEnabled,
            controls.IsPreviousEnabled,
            controls.IsNextEnabled,
            controls.IsShuffleEnabled,
            controls.IsPlaybackPositionEnabled);
    }

    private static async Task<string?> ReadArtworkDataUrlAsync(
        IRandomAccessStreamReference? reference,
        CancellationToken cancellationToken)
    {
        if (reference is null)
        {
            return null;
        }

        using var stream = await reference.OpenReadAsync();
        cancellationToken.ThrowIfCancellationRequested();

        const ulong maximumArtworkBytes = 2 * 1024 * 1024;
        var length = (uint)Math.Min(stream.Size, maximumArtworkBytes);
        if (length == 0)
        {
            return null;
        }

        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync(length);
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = new byte[length];
        reader.ReadBytes(bytes);
        var contentType = string.IsNullOrWhiteSpace(stream.ContentType)
            ? "image/jpeg"
            : stream.ContentType;
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static bool IsPlaying(GlobalSystemMediaTransportControlsSession session)
    {
        return session.GetPlaybackInfo().PlaybackStatus ==
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
    }

    private static bool IsSpotify(string sourceAppId)
    {
        return sourceAppId.Contains("spotify", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatSource(string sourceAppId)
    {
        var source = sourceAppId.Split('!')[0];
        source = Path.GetFileNameWithoutExtension(source);
        return string.IsNullOrWhiteSpace(source) ? "Media" : source;
    }

    private static MediaSummary EmptyState()
    {
        return new MediaSummary(
            "Media",
            "No active session",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            [],
            0,
            0,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed.ToString(),
            string.Empty,
            null,
            string.Empty,
            null,
            null,
            false,
            0,
            0,
            false,
            false,
            false,
            false,
            false);
    }
}
