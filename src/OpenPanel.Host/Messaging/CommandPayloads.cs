namespace OpenPanel.Host.Messaging;

internal sealed record AudioSelectPayload(
    string OutputId,
    bool SetCommunicationsDevice);

internal sealed record AudioVolumePayload(int VolumePercent);

internal sealed record AudioMutePayload(bool IsMuted);

internal sealed record MediaSeekPayload(double PositionSeconds);
