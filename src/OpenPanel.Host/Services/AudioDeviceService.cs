using NAudio.CoreAudioApi;
using OpenPanel.Host.Interop.AudioPolicyConfig;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public interface IAudioDeviceService
{
    Task<AudioSummary> GetOutputsAsync(CancellationToken cancellationToken);
    Task SelectOutputAsync(
        string outputId,
        bool setCommunicationsDevice,
        CancellationToken cancellationToken);
    Task SetVolumeAsync(int volumePercent, CancellationToken cancellationToken);
    Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken);
}

public sealed class AudioDeviceService : IAudioDeviceService
{
    private volatile bool setCommunicationsDevice = true;

    public Task<AudioSummary> GetOutputsAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => ReadState(cancellationToken), cancellationToken);
    }

    public Task SelectOutputAsync(
        string outputId,
        bool setCommunicationsDevice,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputId);
        this.setCommunicationsDevice = setCommunicationsDevice;
        return Task.Run(
            () => AudioDefaultDeviceSwitcher.SetDefaultOutput(
                outputId,
                setCommunicationsDevice),
            cancellationToken);
    }

    public Task SetVolumeAsync(int volumePercent, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.MasterVolumeLevelScalar =
                Math.Clamp(volumePercent, 0, 100) / 100f;
        }, cancellationToken);
    }

    public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.Mute = isMuted;
        }, cancellationToken);
    }

    private AudioSummary ReadState(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var enumerator = new MMDeviceEnumerator();
            using var defaultDevice = enumerator.GetDefaultAudioEndpoint(
                DataFlow.Render,
                Role.Multimedia);
            var defaultId = defaultDevice.ID;
            var outputs = new List<AudioOutputSummary>();

            var endpoints = enumerator.EnumerateAudioEndPoints(
                DataFlow.Render,
                DeviceState.Active);
            foreach (var endpoint in endpoints)
            {
                using (endpoint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    outputs.Add(new AudioOutputSummary(
                        endpoint.ID,
                        endpoint.FriendlyName,
                        string.Equals(endpoint.ID, defaultId, StringComparison.Ordinal)));
                }
            }

            return new AudioSummary(
                defaultId,
                defaultDevice.FriendlyName,
                (int)Math.Round(defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100),
                defaultDevice.AudioEndpointVolume.Mute,
                (int)Math.Round(defaultDevice.AudioMeterInformation.MasterPeakValue * 100),
                setCommunicationsDevice,
                outputs
                    .OrderBy(output => output.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray());
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new AudioSummary(
                null,
                "No active output",
                0,
                false,
                0,
                setCommunicationsDevice,
                Array.Empty<AudioOutputSummary>());
        }
    }
}
