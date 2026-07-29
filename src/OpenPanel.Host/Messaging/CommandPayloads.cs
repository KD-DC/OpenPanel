namespace OpenPanel.Host.Messaging;

internal sealed record AudioSelectPayload(
    string OutputId,
    bool SetCommunicationsDevice);

internal sealed record AudioVolumePayload(int VolumePercent);

internal sealed record AudioMutePayload(bool IsMuted);

internal sealed record AudioExpandedPayload(bool IsExpanded);

internal sealed record AudioInputSelectPayload(string InputId);

internal sealed record AudioInputVolumePayload(int VolumePercent);

internal sealed record AudioInputMutePayload(bool IsMuted);

internal sealed record AudioSessionVolumePayload(
    string SessionId,
    int VolumePercent);

internal sealed record AudioSessionMutePayload(
    string SessionId,
    bool IsMuted);

internal sealed record MediaSeekPayload(double PositionSeconds);

internal sealed record MediaShufflePayload(bool IsActive);
