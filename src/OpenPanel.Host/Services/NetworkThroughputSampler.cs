using System.Diagnostics;
using System.Net.NetworkInformation;

namespace OpenPanel.Host.Services;

internal readonly record struct NetworkRates(double UploadMbps, double DownloadMbps);

internal sealed class NetworkThroughputSampler
{
    private readonly Dictionary<string, NetworkCounters> previousCounters = new(StringComparer.Ordinal);
    private long previousTimestamp;

    public NetworkRates Sample()
    {
        var timestamp = Stopwatch.GetTimestamp();
        var currentCounters = ReadInterfaceCounters();

        if (previousTimestamp == 0)
        {
            ReplacePreviousCounters(currentCounters, timestamp);
            return default;
        }

        var elapsed = Stopwatch.GetElapsedTime(previousTimestamp, timestamp);
        long maxBytesSent = 0;
        long maxBytesReceived = 0;

        foreach (var (interfaceId, current) in currentCounters)
        {
            if (!previousCounters.TryGetValue(interfaceId, out var previous))
            {
                continue;
            }

            maxBytesSent = Math.Max(maxBytesSent, NonNegativeDelta(current.BytesSent, previous.BytesSent));
            maxBytesReceived = Math.Max(
                maxBytesReceived,
                NonNegativeDelta(current.BytesReceived, previous.BytesReceived));
        }

        ReplacePreviousCounters(currentCounters, timestamp);
        return new NetworkRates(
            BytesToMegabitsPerSecond(maxBytesSent, elapsed),
            BytesToMegabitsPerSecond(maxBytesReceived, elapsed));
    }

    internal static double BytesToMegabitsPerSecond(long bytes, TimeSpan elapsed)
    {
        if (bytes <= 0 || elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        return bytes * 8d / 1_000_000d / elapsed.TotalSeconds;
    }

    private static Dictionary<string, NetworkCounters> ReadInterfaceCounters()
    {
        var counters = new Dictionary<string, NetworkCounters>(StringComparer.Ordinal);

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            try
            {
                var statistics = networkInterface.GetIPv4Statistics();
                counters[networkInterface.Id] = new NetworkCounters(
                    statistics.BytesSent,
                    statistics.BytesReceived);
            }
            catch (NetworkInformationException)
            {
                // Interfaces can disappear between enumeration and sampling.
            }
        }

        return counters;
    }

    private void ReplacePreviousCounters(
        IReadOnlyDictionary<string, NetworkCounters> currentCounters,
        long timestamp)
    {
        previousCounters.Clear();
        foreach (var (interfaceId, counters) in currentCounters)
        {
            previousCounters[interfaceId] = counters;
        }

        previousTimestamp = timestamp;
    }

    private static long NonNegativeDelta(long current, long previous)
    {
        return current >= previous ? current - previous : 0;
    }

    private readonly record struct NetworkCounters(long BytesSent, long BytesReceived);
}
