using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class NetworkQualityService
{
    private const string ProbeTarget = "1.1.1.1";
    private const int SampleCapacity = 20;
    private static readonly TimeSpan PingTimeout = TimeSpan.FromMilliseconds(900);

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Queue<NetworkProbeSample> samples = new();
    private volatile bool isActive;

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            lock (samples)
            {
                samples.Clear();
            }
        }
    }

    public async Task<NetworkQualitySummary> GetSnapshotAsync(
        CancellationToken cancellationToken)
    {
        if (!isActive)
        {
            return InactiveState();
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            var networkInterface = SelectActiveInterface();
            if (networkInterface is null)
            {
                return new NetworkQualitySummary(
                    true,
                    false,
                    "No active network",
                    "--",
                    "--",
                    "--",
                    null,
                    null,
                    null,
                    null,
                    ProbeTarget,
                    InactiveApplicationTraffic());
            }

            var sample = await ProbeAsync(cancellationToken);
            NetworkProbeSample[] sampleHistory;
            lock (samples)
            {
                samples.Enqueue(sample);
                while (samples.Count > SampleCapacity)
                {
                    samples.Dequeue();
                }
                sampleHistory = samples.ToArray();
            }

            var metrics = CalculateMetrics(sampleHistory);
            var properties = networkInterface.GetIPProperties();
            var localAddress = properties.UnicastAddresses
                .Select(address => address.Address)
                .FirstOrDefault(address =>
                    address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address));
            return new NetworkQualitySummary(
                true,
                true,
                sample.Success ? "Online" : "Probe timed out",
                networkInterface.Name,
                ConnectionType(networkInterface.NetworkInterfaceType),
                localAddress?.ToString() ?? "--",
                networkInterface.Speed > 0
                    ? networkInterface.Speed / 1_000_000d
                    : null,
                metrics.LatencyMs,
                metrics.JitterMs,
                metrics.PacketLossPercent,
                ProbeTarget,
                InactiveApplicationTraffic());
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new NetworkQualitySummary(
                true,
                false,
                "Diagnostics unavailable",
                "--",
                "--",
                "--",
                null,
                null,
                null,
                null,
                ProbeTarget,
                InactiveApplicationTraffic());
        }
        finally
        {
            gate.Release();
        }
    }

    internal static NetworkQualityMetrics CalculateMetrics(
        IEnumerable<NetworkProbeSample> source)
    {
        var all = source.ToArray();
        var successful = all.Where(sample => sample.Success).ToArray();
        if (all.Length == 0 || successful.Length == 0)
        {
            return new NetworkQualityMetrics(
                null,
                null,
                all.Length == 0 ? null : 100);
        }

        var latency = successful.Average(sample => sample.LatencyMs);
        var jitter = successful.Length < 2
            ? 0
            : successful.Zip(
                    successful.Skip(1),
                    (left, right) => Math.Abs(right.LatencyMs - left.LatencyMs))
                .Average();
        var loss = (all.Length - successful.Length) / (double)all.Length * 100;
        return new NetworkQualityMetrics(latency, jitter, loss);
    }

    private static NetworkQualitySummary InactiveState()
    {
        return new NetworkQualitySummary(
            false,
            false,
            "Open to start diagnostics",
            "--",
            "--",
            "--",
            null,
            null,
            null,
            null,
            ProbeTarget,
            InactiveApplicationTraffic());
    }

    private static NetworkApplicationTrafficSummary InactiveApplicationTraffic()
    {
        return new NetworkApplicationTrafficSummary(
            false,
            false,
            "Open to start app tracking",
            []);
    }

    private static NetworkInterface? SelectActiveInterface()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface =>
                networkInterface.OperationalStatus == OperationalStatus.Up &&
                networkInterface.NetworkInterfaceType is not
                    (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .OrderByDescending(networkInterface =>
            {
                try
                {
                    return networkInterface.GetIPProperties().GatewayAddresses.Count > 0;
                }
                catch (NetworkInformationException)
                {
                    return false;
                }
            })
            .ThenByDescending(networkInterface => networkInterface.Speed)
            .FirstOrDefault();
    }

    private static async Task<NetworkProbeSample> ProbeAsync(
        CancellationToken cancellationToken)
    {
        using var ping = new Ping();
        try
        {
            var reply = await ping.SendPingAsync(
                IPAddress.Parse(ProbeTarget),
                PingTimeout,
                [],
                new PingOptions(),
                cancellationToken);
            return reply.Status == IPStatus.Success
                ? new NetworkProbeSample(true, reply.RoundtripTime)
                : new NetworkProbeSample(false, 0);
        }
        catch (PingException)
        {
            return new NetworkProbeSample(false, 0);
        }
    }

    private static string ConnectionType(NetworkInterfaceType type)
    {
        return type switch
        {
            NetworkInterfaceType.Wireless80211 => "Wi-Fi",
            NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet =>
                "Ethernet",
            _ => type.ToString()
        };
    }
}

internal readonly record struct NetworkProbeSample(bool Success, double LatencyMs);

internal readonly record struct NetworkQualityMetrics(
    double? LatencyMs,
    double? JitterMs,
    double? PacketLossPercent);
