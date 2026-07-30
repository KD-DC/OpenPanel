using HidSharp;
using System.IO;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

internal sealed class LogitechHidBatteryReader
{
    private const int LogitechVendorId = 0x046d;
    private const byte LongReportId = 0x11;
    private const byte SoftwareId = 0x0a;
    private const int ReadTimeoutMilliseconds = 350;
    private const int MaximumReceiverSlot = 6;

    public Task<IReadOnlyList<PeripheralBatteryDeviceSummary>> ReadAsync(
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ReadDevices(cancellationToken),
            cancellationToken);
    }

    private static IReadOnlyList<PeripheralBatteryDeviceSummary> ReadDevices(
        CancellationToken cancellationToken)
    {
        var devices = new List<PeripheralBatteryDeviceSummary>();
        var receivers = DeviceList.Local
            .GetHidDevices(LogitechVendorId)
            .Where(device =>
                device.GetMaxInputReportLength() >= 20 &&
                device.GetMaxOutputReportLength() >= 20)
            .GroupBy(device => device.ProductID)
            .Select(group => group
                .OrderBy(device =>
                    Math.Abs(device.GetMaxInputReportLength() - 20) +
                    Math.Abs(device.GetMaxOutputReportLength() - 20))
                .First());

        foreach (var receiver in receivers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!receiver.TryOpen(out var stream))
            {
                continue;
            }

            using (stream)
            {
                stream.ReadTimeout = ReadTimeoutMilliseconds;
                stream.WriteTimeout = ReadTimeoutMilliseconds;
                for (byte slot = 1; slot <= MaximumReceiverSlot; slot++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!TryPing(stream, slot))
                    {
                        continue;
                    }

                    var name = TryReadName(stream, slot) ??
                        $"Logitech device {slot}";
                    var battery = TryReadBattery(stream, slot);
                    devices.Add(new PeripheralBatteryDeviceSummary(
                        $"logitech:{receiver.ProductID:X4}:{slot}",
                        name,
                        PeripheralBatteryService.Category(name),
                        battery?.Percent,
                        battery?.IsCharging,
                        true,
                        "Logitech HID++",
                        battery?.State));
                }
            }
        }

        return devices;
    }

    private static bool TryPing(HidStream stream, byte slot)
    {
        const byte echo = 0x5a;
        var response = SendReceive(
            stream,
            new HidMessage(slot, 0, 1, [0, 0, echo]),
            retryCount: 1);
        return response is not null && response[6] == echo;
    }

    private static string? TryReadName(HidStream stream, byte slot)
    {
        var feature = TryGetFeatureIndex(stream, slot, 0x0005);
        if (feature is null)
        {
            return null;
        }

        var lengthResponse = SendReceive(
            stream,
            new HidMessage(slot, feature.Value, 0, [0, 0, 0]));
        if (lengthResponse is null || lengthResponse[4] == 0)
        {
            return null;
        }

        var length = lengthResponse[4];
        var bytes = new List<byte>(length);
        while (bytes.Count < length)
        {
            var response = SendReceive(
                stream,
                new HidMessage(
                    slot,
                    feature.Value,
                    1,
                    [(byte)bytes.Count, 0, 0]));
            if (response is null)
            {
                return null;
            }

            for (var index = 4; index <= 6 && bytes.Count < length; index++)
            {
                bytes.Add(response[index]);
            }
        }

        var name = System.Text.Encoding.UTF8
            .GetString(bytes.ToArray())
            .TrimEnd('\0', ' ');
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static LogitechBatteryReading? TryReadBattery(
        HidStream stream,
        byte slot)
    {
        foreach (var featureId in new ushort[] { 0x1004, 0x1000, 0x1001 })
        {
            var feature = TryGetFeatureIndex(stream, slot, featureId);
            if (feature is null)
            {
                continue;
            }

            if (featureId == 0x1004)
            {
                var capabilities = SendReceive(
                    stream,
                    new HidMessage(slot, feature.Value, 0, [0, 0, 0]));
                var batteryInfo = SendReceive(
                    stream,
                    new HidMessage(slot, feature.Value, 1, [0, 0, 0]));
                if (capabilities is null || batteryInfo is null)
                {
                    continue;
                }

                var supportsPercentage = (capabilities[5] & 0x02) != 0;
                return new LogitechBatteryReading(
                    supportsPercentage
                        ? Math.Clamp((int)batteryInfo[4], 0, 100)
                        : null,
                    batteryInfo[6] is 1 or 2,
                    supportsPercentage
                        ? null
                        : UnifiedBatteryState(batteryInfo[5]));
            }

            if (featureId == 0x1000)
            {
                var capabilities = SendReceive(
                    stream,
                    new HidMessage(slot, feature.Value, 1, [0, 0, 0]));
                var batteryInfo = SendReceive(
                    stream,
                    new HidMessage(slot, feature.Value, 0, [0, 0, 0]));
                if (capabilities is null || batteryInfo is null)
                {
                    continue;
                }

                var supportsPercentage =
                    capabilities[4] >= 10 &&
                    (capabilities[5] & 0x02) != 0;
                return new LogitechBatteryReading(
                    supportsPercentage && batteryInfo[4] > 0
                        ? Math.Clamp((int)batteryInfo[4], 0, 100)
                        : null,
                    batteryInfo[6] is 1 or 2 or 3 or 4,
                    supportsPercentage
                        ? null
                        : LegacyBatteryState(batteryInfo[4]));
            }

            var response = SendReceive(
                stream,
                new HidMessage(slot, feature.Value, 0, [0, 0, 0]));
            if (response is null)
            {
                continue;
            }

            var millivolts = (ushort)((response[4] << 8) | response[5]);
            return new LogitechBatteryReading(
                VoltageToPercent(millivolts),
                (response[6] & 0x80) != 0 &&
                (response[6] & 0x07) != 2,
                null);
        }

        return null;
    }

    internal static string? UnifiedBatteryState(byte value)
    {
        return value switch
        {
            1 => "Critical",
            2 => "Low",
            4 => "Good",
            8 => "Full",
            _ => null
        };
    }

    internal static string? LegacyBatteryState(byte value)
    {
        if (value == 0)
        {
            return null;
        }
        if (value <= 10)
        {
            return "Critical";
        }
        if (value <= 30)
        {
            return "Low";
        }
        if (value <= 80)
        {
            return "Good";
        }
        return "Full";
    }

    private static byte? TryGetFeatureIndex(
        HidStream stream,
        byte slot,
        ushort featureId)
    {
        var response = SendReceive(
            stream,
            new HidMessage(
                slot,
                0,
                0,
                [(byte)(featureId >> 8), (byte)featureId, 0]));
        return response is null || response[4] == 0 ? null : response[4];
    }

    private static byte[]? SendReceive(
        HidStream stream,
        HidMessage message,
        int retryCount = 2)
    {
        var request = message.Encode();
        for (var attempt = 0; attempt < retryCount; attempt++)
        {
            try
            {
                stream.Write(request);
                while (true)
                {
                    var buffer = new byte[64];
                    var length = stream.Read(buffer);
                    var response = Normalize(buffer, length);
                    if (response is null)
                    {
                        continue;
                    }
                    if (response[2] == 0xff)
                    {
                        return null;
                    }
                    if (response[1] == message.DeviceIndex &&
                        response[2] == message.FeatureIndex &&
                        (response[3] & 0x0f) == SoftwareId)
                    {
                        return response;
                    }
                }
            }
            catch (TimeoutException)
            {
                // Sleeping wireless devices may miss the first request.
            }
            catch (IOException)
            {
                return null;
            }
        }

        return null;
    }

    private static byte[]? Normalize(byte[] buffer, int length)
    {
        if (length >= 7 &&
            buffer[0] is LongReportId or 0x10)
        {
            return buffer;
        }
        if (length >= 8 &&
            buffer[1] is LongReportId or 0x10)
        {
            return buffer[1..];
        }

        return null;
    }

    internal static int VoltageToPercent(ushort millivolts)
    {
        for (var index = 0; index < VoltageThresholds.Length; index++)
        {
            if (millivolts > VoltageThresholds[index])
            {
                return 100 - index;
            }
        }

        return 0;
    }

    // Curve adapted from the MIT-licensed logitray HID++ implementation.
    private static readonly ushort[] VoltageThresholds =
    [
        4186, 4156, 4143, 4133, 4122, 4113, 4103, 4094, 4086, 4075,
        4067, 4059, 4051, 4043, 4035, 4027, 4019, 4011, 4003, 3997,
        3989, 3983, 3976, 3969, 3961, 3955, 3949, 3942, 3935, 3929,
        3922, 3916, 3909, 3902, 3896, 3890, 3883, 3877, 3870, 3865,
        3859, 3853, 3848, 3842, 3837, 3833, 3828, 3824, 3819, 3815,
        3811, 3808, 3804, 3800, 3797, 3793, 3790, 3787, 3784, 3781,
        3778, 3775, 3772, 3770, 3767, 3764, 3762, 3759, 3757, 3754,
        3751, 3748, 3744, 3741, 3737, 3734, 3730, 3726, 3724, 3720,
        3717, 3714, 3710, 3706, 3702, 3697, 3693, 3688, 3683, 3677,
        3671, 3666, 3662, 3658, 3654, 3646, 3633, 3612, 3579, 3537
    ];

    private readonly record struct HidMessage(
        byte DeviceIndex,
        byte FeatureIndex,
        byte FunctionId,
        byte[] Parameters)
    {
        public byte[] Encode()
        {
            var report = new byte[20];
            report[0] = LongReportId;
            report[1] = DeviceIndex;
            report[2] = FeatureIndex;
            report[3] = (byte)((FunctionId << 4) | SoftwareId);
            Parameters.CopyTo(report, 4);
            return report;
        }
    }

    private readonly record struct LogitechBatteryReading(
        int? Percent,
        bool IsCharging,
        string? State);
}
