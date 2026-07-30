using System.Diagnostics;
using System.Globalization;
using System.IO;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class GamingPerformanceService : IDisposable
{
    private const string SessionName = "OpenPanelGaming";

    private readonly object syncRoot = new();
    private readonly GamingPerformanceAccumulator accumulator = new();
    private readonly string collectorPath;

    private CancellationTokenSource? collectorCancellation;
    private Process? collectorProcess;
    private Task? readerTask;
    private DateTimeOffset? startedAt;
    private string status = "Tap start to monitor a game";
    private bool isActive;
    private bool disposed;

    public GamingPerformanceService()
    {
        collectorPath = Path.Combine(
            AppContext.BaseDirectory,
            "Tools",
            "PresentMon.exe");
    }

    public GamingPerformanceSummary GetSnapshot()
    {
        lock (syncRoot)
        {
            var metrics = accumulator.GetSnapshot();
            return new GamingPerformanceSummary(
                isActive,
                File.Exists(collectorPath),
                status,
                metrics.Application,
                metrics.Fps,
                metrics.FrameTimeMs,
                metrics.OnePercentLowFps,
                metrics.GpuBusyMs,
                metrics.StutterCount,
                startedAt);
        }
    }

    public async Task SetActiveAsync(
        bool active,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (active)
        {
            await StartAsync(cancellationToken);
        }
        else
        {
            await StopAsync();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        StopAsync().GetAwaiter().GetResult();
    }

    private Task StartAsync(CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            if (isActive)
            {
                return Task.CompletedTask;
            }

            if (collectorProcess is { HasExited: true })
            {
                collectorProcess.Dispose();
                collectorProcess = null;
                collectorCancellation?.Dispose();
                collectorCancellation = null;
                readerTask = null;
            }

            if (!File.Exists(collectorPath))
            {
                status = "PresentMon is not installed";
                return Task.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();
            accumulator.Reset();
            collectorCancellation = new CancellationTokenSource();
            collectorProcess = StartCollectorProcess(
                "--output_stdout --no_console_stats --v1_metrics " +
                $"--exclude_dropped --session_name {SessionName}");
            try
            {
                collectorProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch (InvalidOperationException)
            {
                // The process can exit before its priority is assigned.
            }

            isActive = true;
            status = "Waiting for a game";
            startedAt = DateTimeOffset.Now;
            readerTask = ReadCollectorAsync(
                collectorProcess,
                collectorCancellation.Token);
            AppLog.Write("gaming.started", $"pid={collectorProcess.Id}");
            return Task.CompletedTask;
        }
    }

    private async Task StopAsync()
    {
        Process? process;
        Task? reader;
        CancellationTokenSource? cancellation;
        lock (syncRoot)
        {
            process = collectorProcess;
            reader = readerTask;
            cancellation = collectorCancellation;
            collectorProcess = null;
            readerTask = null;
            collectorCancellation = null;
            isActive = false;
            status = "Tap start to monitor a game";
            startedAt = null;
            accumulator.Reset();
        }

        cancellation?.Cancel();
        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                await process.WaitForExitAsync()
                    .WaitAsync(TimeSpan.FromSeconds(2));
                if (!process.HasExited)
                {
                    throw new TimeoutException(
                        "PresentMon did not exit after its process tree was terminated.");
                }
            }
            catch (Exception ex) when (ex is
                InvalidOperationException or
                System.ComponentModel.Win32Exception or
                TimeoutException)
            {
                AppLog.Write(
                    "gaming.stop.warning",
                    $"{ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                process.Dispose();
            }
        }

        if (reader is not null)
        {
            try
            {
                await reader.WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex) when (ex is
                OperationCanceledException or
                TimeoutException)
            {
                // The process has already been terminated.
            }
        }

        cancellation?.Dispose();
        await TerminateTraceSessionAsync();
        AppLog.Write("gaming.stopped", "collector and trace session terminated");
    }

    private async Task ReadCollectorAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        var errorTask = process.StandardError.ReadToEndAsync();
        try
        {
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync(
                       cancellationToken)) is not null)
            {
                lock (syncRoot)
                {
                    if (accumulator.AcceptLine(line))
                    {
                        var snapshot = accumulator.GetSnapshot();
                        status = string.IsNullOrWhiteSpace(snapshot.Application)
                            ? "Waiting for a game"
                            : "Monitoring";
                    }
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                var errorText = await errorTask;
                lock (syncRoot)
                {
                    isActive = false;
                    status = CollectorExitStatus(errorText);
                }
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    AppLog.Write(
                        "gaming.collector.output",
                        errorText.ReplaceLineEndings(" ").Trim());
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Manual stop cancels the output reader.
        }
        catch (Exception ex)
        {
            lock (syncRoot)
            {
                isActive = false;
                status = "Collector failed";
            }
            AppLog.Write(
                "gaming.collector.failed",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    internal static string CollectorExitStatus(string errorText)
    {
        if (errorText.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains(
                "Performance Log Users",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Windows performance access required";
        }

        return string.IsNullOrWhiteSpace(errorText)
            ? "Collector stopped"
            : "Collector failed";
    }

    private async Task TerminateTraceSessionAsync()
    {
        if (!File.Exists(collectorPath))
        {
            return;
        }

        try
        {
            using var terminator = StartCollectorProcess(
                $"--session_name {SessionName} --terminate_existing_session");
            try
            {
                await terminator.WaitForExitAsync()
                    .WaitAsync(TimeSpan.FromSeconds(2));
            }
            finally
            {
                if (!terminator.HasExited)
                {
                    terminator.Kill(entireProcessTree: true);
                    await terminator.WaitForExitAsync()
                        .WaitAsync(TimeSpan.FromSeconds(1));
                }
            }
        }
        catch (Exception ex) when (ex is
            InvalidOperationException or
            System.ComponentModel.Win32Exception or
            TimeoutException)
        {
            AppLog.Write(
                "gaming.session.cleanup.warning",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private Process StartCollectorProcess(string arguments)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = collectorPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(collectorPath)!
        }) ?? throw new InvalidOperationException("PresentMon did not start.");
    }
}

internal sealed class GamingPerformanceAccumulator
{
    private static readonly TimeSpan SelectionWindow = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromSeconds(10);
    private static readonly string[] ExcludedApplications =
    [
        "dwm.exe",
        "explorer.exe",
        "openpanel.host.exe",
        "msedgewebview2.exe",
        "applicationframehost.exe",
        "textinputhost.exe",
        "searchhost.exe"
    ];

    private readonly Dictionary<string, int> columns =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<GamingFrame> frames = [];

    public bool AcceptLine(string line)
    {
        var values = ParseCsv(line);
        if (values.Count == 0)
        {
            return false;
        }

        if (values[0].Equals("Application", StringComparison.OrdinalIgnoreCase))
        {
            columns.Clear();
            for (var index = 0; index < values.Count; index++)
            {
                columns[values[index]] = index;
            }
            return false;
        }

        if (columns.Count == 0)
        {
            return false;
        }

        var application = Read(values, "Application");
        if (string.IsNullOrWhiteSpace(application) ||
            ExcludedApplications.Contains(application, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var frameTime = ReadDouble(
            values,
            "MsBetweenPresents",
            "FrameTime",
            "MsBetweenDisplayChange");
        if (frameTime is null or <= 0 or > 10000)
        {
            return false;
        }

        frames.Add(new GamingFrame(
            application,
            Stopwatch.GetTimestamp(),
            frameTime.Value,
            ReadDouble(values, "MsGPUBusy", "GPUBusy", "MsGPUTime")));
        Prune();
        return true;
    }

    public GamingMetricSnapshot GetSnapshot()
    {
        Prune();
        var now = Stopwatch.GetTimestamp();
        var selected = frames
            .Where(frame =>
                Stopwatch.GetElapsedTime(frame.ObservedAt, now) <= SelectionWindow)
            .GroupBy(frame => frame.Application, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();
        if (selected is null)
        {
            return GamingMetricSnapshot.Empty;
        }

        var application = selected.Key;
        var recent = selected.ToArray();
        var history = frames
            .Where(frame =>
                frame.Application.Equals(application, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var frameTimes = history
            .Select(frame => frame.FrameTimeMs)
            .OrderBy(value => value)
            .ToArray();
        var onePercentCount = Math.Max(1, (int)Math.Ceiling(frameTimes.Length * 0.01));
        var lowFps = frameTimes
            .TakeLast(onePercentCount)
            .Select(value => 1000 / value)
            .Average();
        var median = frameTimes[frameTimes.Length / 2];
        var stutterThreshold = Math.Max(33.3, median * 2);
        return new GamingMetricSnapshot(
            application,
            recent.Length / SelectionWindow.TotalSeconds,
            recent.Average(frame => frame.FrameTimeMs),
            lowFps,
            recent
                .Where(frame => frame.GpuBusyMs.HasValue)
                .Select(frame => frame.GpuBusyMs!.Value)
                .DefaultIfEmpty()
                .Average() is var gpuBusy && gpuBusy > 0
                    ? gpuBusy
                    : null,
            history.Count(frame => frame.FrameTimeMs >= stutterThreshold));
    }

    public void Reset()
    {
        columns.Clear();
        frames.Clear();
    }

    internal static IReadOnlyList<string> ParseCsv(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }
        values.Add(current.ToString());
        return values;
    }

    private void Prune()
    {
        var now = Stopwatch.GetTimestamp();
        frames.RemoveAll(frame =>
            Stopwatch.GetElapsedTime(frame.ObservedAt, now) > HistoryWindow);
    }

    private string Read(IReadOnlyList<string> values, string name)
    {
        return columns.TryGetValue(name, out var index) && index < values.Count
            ? values[index]
            : "";
    }

    private double? ReadDouble(
        IReadOnlyList<string> values,
        params string[] names)
    {
        foreach (var name in names)
        {
            var value = Read(values, name);
            if (double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                return number;
            }
        }

        return null;
    }

    private readonly record struct GamingFrame(
        string Application,
        long ObservedAt,
        double FrameTimeMs,
        double? GpuBusyMs);
}

internal readonly record struct GamingMetricSnapshot(
    string Application,
    double? Fps,
    double? FrameTimeMs,
    double? OnePercentLowFps,
    double? GpuBusyMs,
    int StutterCount)
{
    public static GamingMetricSnapshot Empty =>
        new("", null, null, null, null, 0);
}
