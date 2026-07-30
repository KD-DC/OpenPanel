using System.IO;
using System.Xml.Linq;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

internal sealed class LogitechOptionsDeviceReader
{
    private readonly string devicesDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LogiOptionsPlus",
        "flow",
        "devices");

    public IReadOnlyList<PeripheralBatteryDeviceSummary> Read()
    {
        if (!Directory.Exists(devicesDirectory))
        {
            return [];
        }

        string[] paths;
        try
        {
            paths = Directory.GetFiles(devicesDirectory, "*.xml");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var devices = new List<PeripheralBatteryDeviceSummary>();
        foreach (var path in paths)
        {
            try
            {
                var device = Parse(XDocument.Load(path, LoadOptions.None));
                if (device is not null)
                {
                    devices.Add(device);
                }
            }
            catch (IOException)
            {
                // Options+ can update its cache while OpenPanel is reading it.
            }
            catch (System.Xml.XmlException)
            {
                // Ignore incomplete or unsupported cache entries.
            }
        }

        return devices;
    }

    internal static PeripheralBatteryDeviceSummary? Parse(XDocument document)
    {
        var root = document.Root;
        var id = root?.Attribute("id")?.Value;
        var type = root?.Attribute("type")?.Value;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        var category = type.ToLowerInvariant() switch
        {
            "mouse" => "Mouse",
            "keyboard" => "Keyboard",
            _ => "Other"
        };
        if (category == "Other")
        {
            return null;
        }

        return new PeripheralBatteryDeviceSummary(
            $"logitech-options:{id}",
            $"Logitech {category.ToLowerInvariant()}",
            category,
            null,
            null,
            true,
            "Logi Options+");
    }
}
