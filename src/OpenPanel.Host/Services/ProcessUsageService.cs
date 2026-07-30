using System.Diagnostics;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class ProcessUsageService
{
    private const int ApplicationLimit = 5;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(2);

    private readonly object syncRoot = new();
    private Dictionary<int, ProcessUsageSample> previous = [];
    private ProcessUsageSummary snapshot = InactiveState();
    private long previousTimestamp;
    private volatile bool isActive;

    public void SetActive(bool active)
    {
        lock (syncRoot)
        {
            if (isActive == active)
            {
                return;
            }

            isActive = active;
            previous = [];
            previousTimestamp = 0;
            snapshot = active
                ? new ProcessUsageSummary(true, "Sampling applications", [], [])
                : InactiveState();
        }
    }

    public ProcessUsageSummary GetSnapshot()
    {
        lock (syncRoot)
        {
            if (!isActive)
            {
                return InactiveState();
            }

            var now = Stopwatch.GetTimestamp();
            if (previousTimestamp != 0 &&
                Stopwatch.GetElapsedTime(previousTimestamp, now) < SampleInterval)
            {
                return snapshot;
            }

            var current = CaptureProcesses();
            if (previousTimestamp == 0)
            {
                previous = current;
                previousTimestamp = now;
                snapshot = new ProcessUsageSummary(
                    true,
                    "Sampling applications",
                    [],
                    CreateMemoryRanking(current.Values));
                return snapshot;
            }

            var elapsed = Stopwatch.GetElapsedTime(previousTimestamp, now);
            snapshot = CreateSummary(
                current.Values,
                previous,
                elapsed,
                Environment.ProcessorCount);
            previous = current;
            previousTimestamp = now;
            return snapshot;
        }
    }

    public Task<ProcessUsageSummary> GetSnapshotAsync(
        CancellationToken cancellationToken)
    {
        return !isActive
            ? Task.FromResult(InactiveState())
            : Task.Run(GetSnapshot, cancellationToken);
    }

    internal static ProcessUsageSummary CreateSummary(
        IEnumerable<ProcessUsageSample> current,
        IReadOnlyDictionary<int, ProcessUsageSample> previous,
        TimeSpan elapsed,
        int processorCount)
    {
        var elapsedMilliseconds = Math.Max(1, elapsed.TotalMilliseconds);
        var logicalProcessors = Math.Max(1, processorCount);
        var applications = current
            .GroupBy(sample => sample.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var cpuPercent = group.Sum(sample =>
                {
                    if (!previous.TryGetValue(sample.ProcessId, out var prior))
                    {
                        return 0;
                    }

                    var cpuDelta = sample.TotalProcessorTime - prior.TotalProcessorTime;
                    return cpuDelta <= TimeSpan.Zero
                        ? 0
                        : cpuDelta.TotalMilliseconds /
                            (elapsedMilliseconds * logicalProcessors) * 100;
                });
                var memoryMegabytes = group.Sum(sample => sample.WorkingSetBytes) /
                    (1024d * 1024d);
                return new ProcessUsageApplicationSummary(
                    group.Key,
                    Math.Clamp(cpuPercent, 0, 100),
                    Math.Max(0, memoryMegabytes));
            })
            .ToArray();

        return new ProcessUsageSummary(
            true,
            "Live application usage",
            applications
                .Where(application => application.CpuPercent > 0)
                .OrderByDescending(application => application.CpuPercent)
                .ThenByDescending(application => application.MemoryMegabytes)
                .Take(ApplicationLimit)
                .ToArray(),
            applications
                .OrderByDescending(application => application.MemoryMegabytes)
                .ThenByDescending(application => application.CpuPercent)
                .Take(ApplicationLimit)
                .ToArray());
    }

    private static Dictionary<int, ProcessUsageSample> CaptureProcesses()
    {
        var samples = new Dictionary<int, ProcessUsageSample>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id <= 4)
                    {
                        continue;
                    }

                    samples[process.Id] = new ProcessUsageSample(
                        process.Id,
                        process.ProcessName,
                        process.TotalProcessorTime,
                        process.WorkingSet64);
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                        System.ComponentModel.Win32Exception or
                        NotSupportedException)
                {
                    // Protected or exiting processes are omitted from this sample.
                }
            }
        }

        return samples;
    }

    private static IReadOnlyList<ProcessUsageApplicationSummary> CreateMemoryRanking(
        IEnumerable<ProcessUsageSample> current)
    {
        return current
            .GroupBy(sample => sample.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProcessUsageApplicationSummary(
                group.Key,
                0,
                group.Sum(sample => sample.WorkingSetBytes) / (1024d * 1024d)))
            .OrderByDescending(application => application.MemoryMegabytes)
            .Take(ApplicationLimit)
            .ToArray();
    }

    private static ProcessUsageSummary InactiveState()
    {
        return new ProcessUsageSummary(
            false,
            "Open Hardware to start application sampling",
            [],
            []);
    }
}

internal sealed record ProcessUsageSample(
    int ProcessId,
    string Name,
    TimeSpan TotalProcessorTime,
    long WorkingSetBytes);
