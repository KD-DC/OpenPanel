using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class PeripheralBatteryService : IDisposable
{
    private const string BatteryLifeProperty = "System.Devices.BatteryLife";
    private const string ConnectedProperty = "System.Devices.Aep.IsConnected";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(2);

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly LogitechHidBatteryReader logitechReader = new();
    private readonly LogitechOptionsBatteryReader logitechOptionsReader = new();
    private PeripheralBatterySummary cached = new([], null);
    private DateTimeOffset nextRefresh;

    public async Task<PeripheralBatterySummary> GetSnapshotAsync(
        CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow < nextRefresh)
        {
            return WithOptionsBatteryReadings(cached);
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (DateTimeOffset.UtcNow < nextRefresh)
            {
                return WithOptionsBatteryReadings(cached);
            }

            var standardTask = ReadBluetoothDevicesAsync(cancellationToken);
            var logitechTask = logitechReader.ReadAsync(cancellationToken);
            await Task.WhenAll(standardTask, logitechTask);
            var logitechDevices = logitechTask.Result;

            var devices = standardTask.Result
                .Concat(logitechDevices)
                .Where(device => device.BatteryPercent.HasValue)
                .GroupBy(DeviceKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(device => device.BatteryPercent.HasValue)
                    .First())
                .OrderBy(device => device.Category)
                .ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            cached = new PeripheralBatterySummary(devices, DateTimeOffset.Now);
            nextRefresh = DateTimeOffset.UtcNow + RefreshInterval;
            AppLog.Write(
                "peripherals.refreshed",
                devices.Length == 0
                    ? "no compatible devices"
                    : string.Join(
                        "; ",
                        devices.Select(device =>
                            $"{device.Name}={device.BatteryPercent?.ToString() ?? "--"}% ({device.Source})")));
            return WithOptionsBatteryReadings(cached);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            AppLog.Write(
                "peripherals.refresh.failed",
                $"{ex.GetType().Name}: {ex.Message}");
            nextRefresh = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
            return cached;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        logitechOptionsReader.Dispose();
        gate.Dispose();
    }

    private PeripheralBatterySummary WithOptionsBatteryReadings(
        PeripheralBatterySummary snapshot)
    {
        var devices = snapshot.Devices
            .Concat(logitechOptionsReader.GetSnapshot())
            .Where(device => device.BatteryPercent.HasValue)
            .GroupBy(DeviceKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(device =>
                    device.Source.Equals(
                        "Logi Options+",
                        StringComparison.OrdinalIgnoreCase))
                .First())
            .OrderBy(device => device.Category)
            .ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new PeripheralBatterySummary(devices, snapshot.UpdatedAt);
    }

    private static string DeviceKey(PeripheralBatteryDeviceSummary device)
    {
        var name = new string(device.Name
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
        return $"{device.Category}:{name}";
    }

    private static async Task<IReadOnlyList<PeripheralBatteryDeviceSummary>>
        ReadBluetoothDevicesAsync(CancellationToken cancellationToken)
    {
        var requestedProperties = new[]
        {
            BatteryLifeProperty,
            ConnectedProperty
        };
        var found = new Dictionary<string, PeripheralBatteryDeviceSummary>(
            StringComparer.OrdinalIgnoreCase);

        await AddClassicBluetoothDevicesAsync(
            found,
            requestedProperties,
            cancellationToken);
        await AddBluetoothLeDevicesAsync(
            found,
            requestedProperties,
            cancellationToken);
        await AddGattBatteryServicesAsync(found, cancellationToken);
        return found.Values.ToArray();
    }

    private static async Task AddGattBatteryServicesAsync(
        IDictionary<string, PeripheralBatteryDeviceSummary> found,
        CancellationToken cancellationToken)
    {
        var selector = GattDeviceService.GetDeviceSelectorFromUuid(
            GattServiceUuids.Battery);
        var services = await DeviceInformation.FindAllAsync(selector);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var info in services)
        {
            try
            {
                using var service = await GattDeviceService.FromIdAsync(info.Id);
                cancellationToken.ThrowIfCancellationRequested();
                var device = service?.Device;
                if (service is null ||
                    device is null ||
                    string.IsNullOrWhiteSpace(device.Name) ||
                    Category(device.Name) == "Other")
                {
                    continue;
                }

                var battery = await TryReadGattBatteryAsync(
                    service,
                    cancellationToken);
                if (!battery.HasValue)
                {
                    continue;
                }

                found[NormalizeBluetoothId(device.DeviceId, device.Name)] =
                    CreateDevice(
                        device.DeviceId,
                        device.Name,
                        battery,
                        null,
                        device.ConnectionStatus ==
                            BluetoothConnectionStatus.Connected);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // A sleeping device can disappear while services are enumerated.
            }
        }
    }

    private static async Task AddClassicBluetoothDevicesAsync(
        IDictionary<string, PeripheralBatteryDeviceSummary> found,
        IEnumerable<string> requestedProperties,
        CancellationToken cancellationToken)
    {
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(
            selector,
            requestedProperties,
            DeviceInformationKind.AssociationEndpoint);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var device in devices.Where(IsRelevantPeripheral))
        {
            var connected = GetBoolean(device.Properties, ConnectedProperty);
            var battery = GetBatteryPercent(device.Properties);
            found[NormalizeBluetoothId(device.Id, device.Name)] =
                CreateDevice(device.Id, device.Name, battery, null, connected);
        }
    }

    private static async Task AddBluetoothLeDevicesAsync(
        IDictionary<string, PeripheralBatteryDeviceSummary> found,
        IEnumerable<string> requestedProperties,
        CancellationToken cancellationToken)
    {
        var selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(
            selector,
            requestedProperties,
            DeviceInformationKind.AssociationEndpoint);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var info in devices.Where(IsRelevantPeripheral))
        {
            var connected = GetBoolean(info.Properties, ConnectedProperty);
            var battery = GetBatteryPercent(info.Properties);
            if (connected)
            {
                var gattBattery = await TryReadGattBatteryAsync(
                    info.Id,
                    cancellationToken);
                battery = gattBattery ?? battery;
            }

            found[NormalizeBluetoothId(info.Id, info.Name)] =
                CreateDevice(info.Id, info.Name, battery, null, connected);
        }
    }

    private static async Task<int?> TryReadGattBatteryAsync(
        string deviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var device = await BluetoothLEDevice.FromIdAsync(deviceId);
            cancellationToken.ThrowIfCancellationRequested();
            if (device is null ||
                device.ConnectionStatus != BluetoothConnectionStatus.Connected)
            {
                return null;
            }

            var services = await device.GetGattServicesForUuidAsync(
                GattServiceUuids.Battery,
                BluetoothCacheMode.Uncached);
            if (services.Status != GattCommunicationStatus.Success)
            {
                return null;
            }

            foreach (var service in services.Services)
            {
                using (service)
                {
                    var battery = await TryReadGattBatteryAsync(
                        service,
                        cancellationToken);
                    if (battery.HasValue)
                    {
                        return battery;
                    }
                }
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Bluetooth devices can sleep or disconnect during a GATT query.
        }

        return null;
    }

    private static async Task<int?> TryReadGattBatteryAsync(
        GattDeviceService service,
        CancellationToken cancellationToken)
    {
        var characteristics = await service.GetCharacteristicsForUuidAsync(
            GattCharacteristicUuids.BatteryLevel,
            BluetoothCacheMode.Uncached);
        if (characteristics.Status != GattCommunicationStatus.Success)
        {
            return null;
        }

        foreach (var characteristic in characteristics.Characteristics)
        {
            var result = await characteristic.ReadValueAsync(
                BluetoothCacheMode.Uncached);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status != GattCommunicationStatus.Success ||
                result.Value.Length == 0)
            {
                continue;
            }

            using var reader = DataReader.FromBuffer(result.Value);
            return Math.Clamp((int)reader.ReadByte(), 0, 100);
        }
        return null;
    }

    private static PeripheralBatteryDeviceSummary CreateDevice(
        string id,
        string name,
        int? battery,
        bool? charging,
        bool connected)
    {
        return new PeripheralBatteryDeviceSummary(
            NormalizeBluetoothId(id, name),
            CleanBluetoothName(name),
            Category(name),
            battery,
            charging,
            connected,
            "Windows Bluetooth");
    }

    private static bool IsRelevantPeripheral(DeviceInformation device)
    {
        return !string.IsNullOrWhiteSpace(device.Name) &&
            Category(device.Name) != "Other";
    }

    private static bool GetBoolean(
        IReadOnlyDictionary<string, object> properties,
        string name)
    {
        return properties.TryGetValue(name, out var value) &&
            value is bool result &&
            result;
    }

    private static int? GetBatteryPercent(
        IReadOnlyDictionary<string, object> properties)
    {
        if (!properties.TryGetValue(BatteryLifeProperty, out var value) ||
            value is null)
        {
            return null;
        }

        return value switch
        {
            byte number => number,
            sbyte number => Math.Clamp((int)number, 0, 100),
            short number => Math.Clamp((int)number, 0, 100),
            ushort number => Math.Clamp((int)number, 0, 100),
            int number => Math.Clamp(number, 0, 100),
            uint number => (int)Math.Clamp(number, 0u, 100u),
            _ => null
        };
    }

    private static string NormalizeBluetoothId(string id, string name)
    {
        var address = new string(id
            .Where(Uri.IsHexDigit)
            .TakeLast(12)
            .ToArray());
        return address.Length == 12
            ? $"bluetooth:{address.ToUpperInvariant()}"
            : $"bluetooth:{CleanBluetoothName(name).ToUpperInvariant()}";
    }

    private static string CleanBluetoothName(string name)
    {
        return name.StartsWith("LE-", StringComparison.OrdinalIgnoreCase)
            ? name[3..]
            : name;
    }

    internal static string Category(string name)
    {
        if (ContainsAny(
                name,
                "mouse",
                "trackball",
                "master",
                "anywhere",
                "lift",
                "superlight",
                "g502",
                "g703",
                "g903"))
        {
            return "Mouse";
        }
        if (ContainsAny(name, "keyboard", "keys", "craft", "k780", "g915"))
        {
            return "Keyboard";
        }
        if (ContainsAny(name, "headphone", "headset", "earbuds", "bose"))
        {
            return "Headphones";
        }
        if (ContainsAny(name, "controller", "gamepad", "xbox", "dualsense"))
        {
            return "Controller";
        }

        return "Other";
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate =>
            value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }
}
