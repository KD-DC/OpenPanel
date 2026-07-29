using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
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
    void SetExtendedState(bool isExpanded);
    Task SelectInputAsync(string inputId, CancellationToken cancellationToken);
    Task SetInputVolumeAsync(int volumePercent, CancellationToken cancellationToken);
    Task SetInputMutedAsync(bool isMuted, CancellationToken cancellationToken);
    Task SetSessionVolumeAsync(
        string sessionId,
        int volumePercent,
        CancellationToken cancellationToken);
    Task SetSessionMutedAsync(
        string sessionId,
        bool isMuted,
        CancellationToken cancellationToken);
}

public sealed class AudioDeviceService : IAudioDeviceService
{
    private volatile bool includeExtendedState;
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

    public void SetExtendedState(bool isExpanded)
    {
        includeExtendedState = isExpanded;
    }

    public Task SelectInputAsync(string inputId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputId);
        return Task.Run(
            () => AudioDefaultDeviceSwitcher.SetDefaultInput(inputId),
            cancellationToken);
    }

    public Task SetInputVolumeAsync(int volumePercent, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            device.AudioEndpointVolume.MasterVolumeLevelScalar =
                Math.Clamp(volumePercent, 0, 100) / 100f;
        }, cancellationToken);
    }

    public Task SetInputMutedAsync(bool isMuted, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            device.AudioEndpointVolume.Mute = isMuted;
        }, cancellationToken);
    }

    public Task SetSessionVolumeAsync(
        string sessionId,
        int volumePercent,
        CancellationToken cancellationToken)
    {
        return UpdateSessionsAsync(
            sessionId,
            volume => volume.Volume = Math.Clamp(volumePercent, 0, 100) / 100f,
            cancellationToken);
    }

    public Task SetSessionMutedAsync(
        string sessionId,
        bool isMuted,
        CancellationToken cancellationToken)
    {
        return UpdateSessionsAsync(
            sessionId,
            volume => volume.Mute = isMuted,
            cancellationToken);
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

            var inputState = includeExtendedState
                ? ReadInputState(enumerator, cancellationToken)
                : InputState.Empty;
            var sessions = includeExtendedState
                ? ReadSessions(defaultDevice, cancellationToken)
                : Array.Empty<AudioSessionSummary>();

            return new AudioSummary(
                defaultId,
                defaultDevice.FriendlyName,
                (int)Math.Round(defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100),
                defaultDevice.AudioEndpointVolume.Mute,
                (int)Math.Round(defaultDevice.AudioMeterInformation.MasterPeakValue * 100),
                setCommunicationsDevice,
                outputs
                    .OrderBy(output => output.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray(),
                inputState.CurrentId,
                inputState.CurrentName,
                inputState.VolumePercent,
                inputState.IsMuted,
                inputState.PeakLevelPercent,
                inputState.Inputs,
                sessions);
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
                Array.Empty<AudioOutputSummary>(),
                null,
                "No active input",
                0,
                false,
                0,
                Array.Empty<AudioInputSummary>(),
                Array.Empty<AudioSessionSummary>());
        }
    }

    private static InputState ReadInputState(
        MMDeviceEnumerator enumerator,
        CancellationToken cancellationToken)
    {
        try
        {
            using var defaultInput = enumerator.GetDefaultAudioEndpoint(
                DataFlow.Capture,
                Role.Multimedia);
            var defaultId = defaultInput.ID;
            var inputs = new List<AudioInputSummary>();

            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(
                         DataFlow.Capture,
                         DeviceState.Active))
            {
                using (endpoint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    inputs.Add(new AudioInputSummary(
                        endpoint.ID,
                        endpoint.FriendlyName,
                        string.Equals(endpoint.ID, defaultId, StringComparison.Ordinal)));
                }
            }

            return new InputState(
                defaultId,
                defaultInput.FriendlyName,
                (int)Math.Round(defaultInput.AudioEndpointVolume.MasterVolumeLevelScalar * 100),
                defaultInput.AudioEndpointVolume.Mute,
                (int)Math.Round(defaultInput.AudioMeterInformation.MasterPeakValue * 100),
                inputs
                    .OrderBy(input => input.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray());
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new InputState(
                null,
                "No active input",
                0,
                false,
                0,
                Array.Empty<AudioInputSummary>());
        }
    }

    private static IReadOnlyList<AudioSessionSummary> ReadSessions(
        MMDevice defaultDevice,
        CancellationToken cancellationToken)
    {
        var sessions = new Dictionary<string, SessionAccumulator>(StringComparer.Ordinal);
        var sessionManager = defaultDevice.AudioSessionManager;
        try
        {
            var sessionCollection = sessionManager.Sessions;

            for (var index = 0; index < sessionCollection.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var session = sessionCollection[index];
                if (session.State != AudioSessionState.AudioSessionStateActive)
                {
                    continue;
                }

                var key = GetSessionKey(session);
                var name = GetSessionName(session);
                using var simpleVolume = session.SimpleAudioVolume;
                var volume = (int)Math.Round(simpleVolume.Volume * 100);
                var peak = (int)Math.Round(session.AudioMeterInformation.MasterPeakValue * 100);
                var muted = simpleVolume.Mute;

                if (sessions.TryGetValue(key, out var existing))
                {
                    sessions[key] = existing with
                    {
                        VolumeTotal = existing.VolumeTotal + volume,
                        SessionCount = existing.SessionCount + 1,
                        IsMuted = existing.IsMuted && muted,
                        PeakLevelPercent = Math.Max(existing.PeakLevelPercent, peak)
                    };
                }
                else
                {
                    sessions[key] = new SessionAccumulator(
                        key,
                        name,
                        volume,
                        1,
                        muted,
                        peak);
                }
            }
        }
        finally
        {
            sessionManager.Dispose();
        }

        return sessions.Values
            .OrderByDescending(session => session.PeakLevelPercent)
            .ThenBy(session => session.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(5)
            .Select(session => new AudioSessionSummary(
                session.Id,
                session.Name,
                (int)Math.Round((double)session.VolumeTotal / session.SessionCount),
                session.IsMuted,
                session.PeakLevelPercent))
            .ToArray();
    }

    private static Task UpdateSessionsAsync(
        string sessionId,
        Action<SimpleAudioVolume> update,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessionManager = device.AudioSessionManager;
            try
            {
                var sessions = sessionManager.Sessions;

                for (var index = 0; index < sessions.Count; index++)
                {
                    using var session = sessions[index];
                    if (string.Equals(GetSessionKey(session), sessionId, StringComparison.Ordinal))
                    {
                        using var simpleVolume = session.SimpleAudioVolume;
                        update(simpleVolume);
                    }
                }
            }
            finally
            {
                sessionManager.Dispose();
            }
        }, cancellationToken);
    }

    private static string GetSessionKey(AudioSessionControl session)
    {
        return session.IsSystemSoundsSession
            ? "system"
            : $"process:{session.GetProcessID}";
    }

    private static string GetSessionName(AudioSessionControl session)
    {
        if (session.IsSystemSoundsSession)
        {
            return "System Sounds";
        }

        try
        {
            using var process = Process.GetProcessById((int)session.GetProcessID);
            return process.ProcessName;
        }
        catch
        {
            return string.IsNullOrWhiteSpace(session.DisplayName)
                ? "Application"
                : session.DisplayName;
        }
    }

    private sealed record InputState(
        string? CurrentId,
        string CurrentName,
        int VolumePercent,
        bool IsMuted,
        int PeakLevelPercent,
        IReadOnlyList<AudioInputSummary> Inputs)
    {
        public static InputState Empty { get; } = new(
            null,
            "Open expanded controls",
            0,
            false,
            0,
            Array.Empty<AudioInputSummary>());
    }

    private sealed record SessionAccumulator(
        string Id,
        string Name,
        int VolumeTotal,
        int SessionCount,
        bool IsMuted,
        int PeakLevelPercent);
}
