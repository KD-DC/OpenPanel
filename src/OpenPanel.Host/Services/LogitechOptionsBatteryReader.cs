using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

internal sealed class LogitechOptionsBatteryReader : IDisposable
{
    private const string PipePrefix = "logitech_kiros_agent-";
    private const int MaximumPacketBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(10);
    private static readonly string[] SubscriptionPaths =
    [
        "/devices/state/changed",
        "/battery/state/changed",
        "/devices/options/device_arrival",
        "/devices/options/device_removal"
    ];

    private readonly Lock sync = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly Dictionary<string, DeviceRecord> devices =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BatteryRecord> batteries =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Task worker;

    public LogitechOptionsBatteryReader()
    {
        worker = RunAsync(cancellation.Token);
    }

    public IReadOnlyList<PeripheralBatteryDeviceSummary> GetSnapshot()
    {
        lock (sync)
        {
            return devices.Values
                .Where(device =>
                    batteries.TryGetValue(device.Id, out var battery) &&
                    battery.Percent.HasValue)
                .Select(device =>
                {
                    var battery = batteries[device.Id];
                    return new PeripheralBatteryDeviceSummary(
                        $"logitech-options:{device.Id}",
                        device.Name,
                        device.Category,
                        battery.Percent,
                        battery.IsCharging,
                        device.IsConnected,
                        "Logi Options+");
                })
                .ToArray();
        }
    }

    public void Dispose()
    {
        cancellation.Cancel();
        try
        {
            worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex) when (
            ex.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
        }
        if (worker.IsCompleted)
        {
            cancellation.Dispose();
        }
    }

    internal static IReadOnlyList<OptionsDevice> ParseDevices(JsonElement payload)
    {
        if (!payload.TryGetProperty("deviceInfos", out var deviceInfos) ||
            deviceInfos.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<OptionsDevice>();
        foreach (var info in deviceInfos.EnumerateArray())
        {
            var id = ReadString(info, "id");
            var name = ReadString(info, "displayName");
            var type = ReadString(info, "deviceType");
            var state = ReadString(info, "state");
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(name) ||
                type is not ("MOUSE" or "KEYBOARD") ||
                !HasBatteryCapability(info))
            {
                continue;
            }

            parsed.Add(new OptionsDevice(
                id,
                name,
                type == "MOUSE" ? "Mouse" : "Keyboard",
                state is "ACTIVE" or "PRESENT"));
        }
        return parsed;
    }

    internal static OptionsBattery? ParseBattery(JsonElement payload)
    {
        var deviceId = ReadString(payload, "deviceId");
        if (string.IsNullOrWhiteSpace(deviceId) ||
            !payload.TryGetProperty("percentage", out var percentage) ||
            !percentage.TryGetInt32(out var percent))
        {
            return null;
        }

        bool? charging = null;
        if (payload.TryGetProperty("charging", out var chargingValue) &&
            chargingValue.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            charging = chargingValue.GetBoolean();
        }

        return new OptionsBattery(
            deviceId,
            Math.Clamp(percent, 0, 100),
            charging);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pipeName = FindPipeName();
                if (pipeName is null)
                {
                    await Task.Delay(ReconnectDelay, cancellationToken);
                    continue;
                }

                await using var pipe = new NamedPipeClientStream(
                    ".",
                    pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);
                await pipe.ConnectAsync(1000, cancellationToken);
                _ = await ReadPacketAsync(pipe, cancellationToken);
                AppLog.Write(
                    "peripherals.logitech-options.connected",
                    pipeName);

                foreach (var path in SubscriptionPaths)
                {
                    await SendAsync(
                        pipe,
                        Guid.NewGuid().ToString("N"),
                        "SUBSCRIBE",
                        path,
                        cancellationToken);
                }
                await RequestDevicesAsync(pipe, cancellationToken);

                while (!cancellationToken.IsCancellationRequested &&
                       pipe.IsConnected)
                {
                    var message = await ReadPacketAsync(pipe, cancellationToken);
                    if (message is null)
                    {
                        break;
                    }
                    await HandleMessageAsync(pipe, message.Value, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (
                ex is IOException or TimeoutException or UnauthorizedAccessException)
            {
                AppLog.Write(
                    "peripherals.logitech-options.disconnected",
                    $"{ex.GetType().Name}: {ex.Message}");
            }

            try
            {
                await Task.Delay(ReconnectDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task HandleMessageAsync(
        Stream pipe,
        JsonElement message,
        CancellationToken cancellationToken)
    {
        var path = ReadString(message, "path");
        if (!message.TryGetProperty("payload", out var payload))
        {
            return;
        }

        if (path == "/devices/list")
        {
            var parsed = ParseDevices(payload);
            lock (sync)
            {
                foreach (var device in parsed)
                {
                    devices[device.Id] = new DeviceRecord(
                        device.Id,
                        device.Name,
                        device.Category,
                        device.IsConnected);
                }
            }

            foreach (var device in parsed.Where(device => device.IsConnected))
            {
                await SendAsync(
                    pipe,
                    Guid.NewGuid().ToString("N"),
                    "GET",
                    $"/battery/{device.Id}/state",
                    cancellationToken);
            }
            return;
        }

        if (path == "/battery/state/changed" ||
            path.StartsWith("/battery/", StringComparison.OrdinalIgnoreCase))
        {
            var battery = ParseBattery(payload);
            if (battery is not null)
            {
                lock (sync)
                {
                    batteries[battery.DeviceId] = new BatteryRecord(
                        battery.Percent,
                        battery.IsCharging);
                }
                AppLog.Write(
                    "peripherals.logitech-options.battery",
                    $"{battery.DeviceId}={battery.Percent}%");
            }
            return;
        }

        if (path.StartsWith("/devices/", StringComparison.OrdinalIgnoreCase))
        {
            await RequestDevicesAsync(pipe, cancellationToken);
        }
    }

    private static async Task RequestDevicesAsync(
        Stream pipe,
        CancellationToken cancellationToken)
    {
        await SendAsync(
            pipe,
            $"openpanel-devices-{Guid.NewGuid():N}",
            "GET",
            "/devices/list",
            cancellationToken);
    }

    private static string? FindPipeName()
    {
        try
        {
            const string pipeRoot = @"\\.\pipe\";
            var path = Directory.GetFiles(pipeRoot)
                .FirstOrDefault(candidate =>
                    candidate.Contains(PipePrefix, StringComparison.OrdinalIgnoreCase));
            return path is null
                ? null
                : path[(path.LastIndexOf('\\') + 1)..];
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static async Task SendAsync(
        Stream stream,
        string messageId,
        string verb,
        string path,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new
        {
            msg_id = messageId,
            verb,
            path
        });
        var type = "json"u8;
        var packetLength = 4 + type.Length + 4 + json.Length;
        var packet = new byte[4 + packetLength];
        BinaryPrimitives.WriteInt32LittleEndian(packet, packetLength);
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(4), type.Length);
        type.CopyTo(packet.AsSpan(8));
        var jsonHeader = 8 + type.Length;
        BinaryPrimitives.WriteInt32BigEndian(
            packet.AsSpan(jsonHeader),
            json.Length);
        json.CopyTo(packet.AsSpan(jsonHeader + 4));
        await stream.WriteAsync(packet, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<JsonElement?> ReadPacketAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[4];
        if (!await ReadExactlyAsync(stream, header, cancellationToken))
        {
            return null;
        }
        var packetLength = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (packetLength <= 0 || packetLength > MaximumPacketBytes)
        {
            throw new IOException($"Invalid Options+ packet length {packetLength}.");
        }

        var packet = new byte[packetLength];
        if (!await ReadExactlyAsync(stream, packet, cancellationToken))
        {
            return null;
        }

        var offset = 0;
        while (offset + 4 <= packet.Length)
        {
            var frameLength = BinaryPrimitives.ReadInt32BigEndian(
                packet.AsSpan(offset, 4));
            offset += 4;
            if (frameLength < 0 || offset + frameLength > packet.Length)
            {
                throw new IOException("Invalid Options+ frame length.");
            }

            var frame = packet.AsSpan(offset, frameLength);
            offset += frameLength;
            if (frame.SequenceEqual("json"u8))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(frame.ToArray());
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                // The initial server handshake includes a protobuf frame.
            }
        }
        return null;
    }

    private static async Task<bool> ReadExactlyAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[offset..], cancellationToken);
            if (read == 0)
            {
                return false;
            }
            offset += read;
        }
        return true;
    }

    private static bool HasBatteryCapability(JsonElement info)
    {
        return info.TryGetProperty("capabilities", out var capabilities) &&
            capabilities.TryGetProperty("hasBatteryStatus", out var value) &&
            value.ValueKind == JsonValueKind.True;
    }

    private static string ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
    }

    internal sealed record OptionsDevice(
        string Id,
        string Name,
        string Category,
        bool IsConnected);

    internal sealed record OptionsBattery(
        string DeviceId,
        int Percent,
        bool? IsCharging);

    private sealed record DeviceRecord(
        string Id,
        string Name,
        string Category,
        bool IsConnected);

    private sealed record BatteryRecord(int? Percent, bool? IsCharging);
}
