namespace OpenPanel.Host.Services;

internal readonly record struct MediaSessionCandidate(
    string SourceAppId,
    bool IsPlaying);

internal static class MediaSessionSelector
{
    public static int SelectIndex(
        IReadOnlyList<MediaSessionCandidate> candidates,
        string? lastSourceAppId,
        string? currentSourceAppId)
    {
        if (candidates.Count == 0)
        {
            return -1;
        }

        var index = Find(candidates, candidate =>
            candidate.IsPlaying && IsSpotify(candidate.SourceAppId));
        if (index >= 0)
        {
            return index;
        }

        index = Find(candidates, candidate => candidate.IsPlaying);
        if (index >= 0)
        {
            return index;
        }

        index = FindSource(candidates, lastSourceAppId);
        if (index >= 0)
        {
            return index;
        }

        index = Find(candidates, candidate => IsSpotify(candidate.SourceAppId));
        if (index >= 0)
        {
            return index;
        }

        index = FindSource(candidates, currentSourceAppId);
        return index >= 0 ? index : 0;
    }

    private static int FindSource(
        IReadOnlyList<MediaSessionCandidate> candidates,
        string? sourceAppId)
    {
        return string.IsNullOrWhiteSpace(sourceAppId)
            ? -1
            : Find(candidates, candidate => string.Equals(
                candidate.SourceAppId,
                sourceAppId,
                StringComparison.OrdinalIgnoreCase));
    }

    private static int Find(
        IReadOnlyList<MediaSessionCandidate> candidates,
        Func<MediaSessionCandidate, bool> predicate)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            if (predicate(candidates[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSpotify(string sourceAppId)
    {
        return sourceAppId.Contains("spotify", StringComparison.OrdinalIgnoreCase);
    }
}
