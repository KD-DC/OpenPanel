using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class NetworkApplicationTrafficService : IDisposable
{
    private static readonly TimeSpan AggregationInterval = TimeSpan.FromSeconds(2);
    private static readonly Guid NetworkProviderId =
        new("7dd42a49-5329-4832-8dfd-43d979153a88");
    private static readonly int[] TrafficEventIds = [10, 11, 26, 27, 42, 43, 58, 59];
    private const int MaximumApplications = 8;
    private const ulong NetworkKeywords = 0x30;

    private readonly Lock gate = new();
    private readonly Dictionary<int, MutableTraffic> pending = [];
    private readonly string sessionName = $"OpenPanel-Network-{Environment.ProcessId}";

    private TraceEventSession? session;
    private Task? processingTask;
    private IReadOnlyList<NetworkApplicationSummary> applications = [];
    private long aggregationStarted;
    private string status = "Open to start app tracking";
    private bool isActive;
    private bool isAvailable;
    private bool disposed;

    public void SetActive(bool active)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (active == isActive)
            {
                return;
            }

            if (active)
            {
                StartLocked();
            }
            else
            {
                StopLocked();
            }
        }
    }

    public NetworkApplicationTrafficSummary GetSnapshot()
    {
        lock (gate)
        {
            if (isActive &&
                isAvailable &&
                Stopwatch.GetElapsedTime(aggregationStarted) >= AggregationInterval)
            {
                applications = CreateSnapshotLocked(
                    pending,
                    Stopwatch.GetElapsedTime(aggregationStarted));
                pending.Clear();
                aggregationStarted = Stopwatch.GetTimestamp();
                status = applications.Count == 0
                    ? "Waiting for app traffic"
                    : "Live app traffic";
            }

            return new NetworkApplicationTrafficSummary(
                isActive,
                isAvailable,
                status,
                applications);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            StopLocked();
            disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    internal static IReadOnlyList<NetworkApplicationSummary> CreateSnapshot(
        IReadOnlyDictionary<int, NetworkTrafficCounters> counters,
        TimeSpan elapsed,
        Func<int, string?>? processNameResolver = null)
    {
        var mutable = counters.ToDictionary(
            pair => pair.Key,
            pair => new MutableTraffic(
                pair.Value.Name,
                pair.Value.UploadBytes,
                pair.Value.DownloadBytes));
        return CreateSnapshotCore(
            mutable,
            elapsed,
            processNameResolver ?? ResolveProcessName);
    }

    private void StartLocked()
    {
        isActive = true;
        applications = [];
        pending.Clear();
        aggregationStarted = Stopwatch.GetTimestamp();

        try
        {
            session = new TraceEventSession(sessionName)
            {
                StopOnDispose = true,
                BufferSizeMB = 2
            };

            session.Source.Dynamic.All += HandleNetworkEvent;
            session.EnableProvider(
                NetworkProviderId,
                TraceEventLevel.Informational,
                NetworkKeywords,
                new TraceEventProviderOptions
                {
                    EventIDsToEnable = TrafficEventIds
                });
            processingTask = Task.Factory.StartNew(
                session.Source.Process,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            isAvailable = true;
            status = "Collecting app traffic";
            AppLog.Write("network.apps.started", "2 MB ETW session; 2 second aggregation");
        }
        catch (UnauthorizedAccessException exception)
        {
            CleanupSessionLocked();
            isAvailable = false;
            status = "Sign out and back in to enable app tracking";
            AppLog.Write("network.apps.denied", exception.Message);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 5)
        {
            CleanupSessionLocked();
            isAvailable = false;
            status = "Sign out and back in to enable app tracking";
            AppLog.Write("network.apps.denied", exception.Message);
        }
        catch (Exception exception)
        {
            CleanupSessionLocked();
            isAvailable = false;
            status = $"App tracking unavailable ({exception.GetType().Name})";
            AppLog.Write(
                "network.apps.failed",
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private void StopLocked()
    {
        isActive = false;
        isAvailable = false;
        status = "Open to start app tracking";
        applications = [];
        pending.Clear();
        CleanupSessionLocked();
    }

    private void CleanupSessionLocked()
    {
        var activeSession = session;
        session = null;
        processingTask = null;

        if (activeSession is null)
        {
            return;
        }

        try
        {
            activeSession.Source.StopProcessing();
            activeSession.Dispose();
        }
        catch (Exception)
        {
            // The session may already have stopped after an initialization failure.
        }
    }

    private void AddTraffic(int processId, string? processName, int uploadBytes, int downloadBytes)
    {
        if (processId <= 0 || (uploadBytes <= 0 && downloadBytes <= 0))
        {
            return;
        }

        lock (gate)
        {
            if (!isActive || !isAvailable)
            {
                return;
            }

            if (!pending.TryGetValue(processId, out var traffic))
            {
                traffic = new MutableTraffic(NormalizeProcessName(processName), 0, 0);
                pending[processId] = traffic;
            }

            traffic.UploadBytes += Math.Max(0, uploadBytes);
            traffic.DownloadBytes += Math.Max(0, downloadBytes);
        }
    }

    private void HandleNetworkEvent(TraceEvent data)
    {
        if (data.ProviderGuid != NetworkProviderId)
        {
            return;
        }

        var eventId = (int)data.ID;
        var isUpload = eventId is 10 or 26 or 42 or 58;
        var isDownload = eventId is 11 or 27 or 43 or 59;
        if (!isUpload && !isDownload)
        {
            return;
        }

        try
        {
            var processId = Convert.ToInt32(data.PayloadByName("PID"));
            var byteCount = Convert.ToInt32(data.PayloadByName("size"));
            AddTraffic(
                processId,
                data.ProcessName,
                isUpload ? byteCount : 0,
                isDownload ? byteCount : 0);
        }
        catch (Exception exception) when (
            exception is ArgumentException or FormatException or InvalidCastException or OverflowException)
        {
            // Ignore malformed provider events rather than interrupting the trace consumer.
        }
    }

    private static IReadOnlyList<NetworkApplicationSummary> CreateSnapshotLocked(
        IReadOnlyDictionary<int, MutableTraffic> counters,
        TimeSpan elapsed)
    {
        return CreateSnapshotCore(counters, elapsed, ResolveProcessName);
    }

    private static IReadOnlyList<NetworkApplicationSummary> CreateSnapshotCore(
        IReadOnlyDictionary<int, MutableTraffic> counters,
        TimeSpan elapsed,
        Func<int, string?> processNameResolver)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return [];
        }

        return counters
            .Where(pair => pair.Value.UploadBytes > 0 || pair.Value.DownloadBytes > 0)
            .Select(pair =>
            {
                var name = pair.Value.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = processNameResolver(pair.Key);
                }

                return new NetworkApplicationSummary(
                    pair.Key,
                    string.IsNullOrWhiteSpace(name) ? $"Process {pair.Key}" : name,
                    NetworkThroughputSampler.BytesToMegabitsPerSecond(
                        pair.Value.UploadBytes,
                        elapsed),
                    NetworkThroughputSampler.BytesToMegabitsPerSecond(
                        pair.Value.DownloadBytes,
                        elapsed));
            })
            .OrderByDescending(app => app.UploadMbps + app.DownloadMbps)
            .Take(MaximumApplications)
            .ToArray();
    }

    private static string? ResolveProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return NormalizeProcessName(process.ProcessName);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    private static string? NormalizeProcessName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        return Path.GetFileNameWithoutExtension(trimmed);
    }

    private sealed class MutableTraffic(
        string? name,
        long uploadBytes,
        long downloadBytes)
    {
        public string? Name { get; } = name;
        public long UploadBytes { get; set; } = uploadBytes;
        public long DownloadBytes { get; set; } = downloadBytes;
    }
}

internal readonly record struct NetworkTrafficCounters(
    string? Name,
    long UploadBytes,
    long DownloadBytes);
