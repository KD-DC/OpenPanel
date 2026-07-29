using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public interface ITelemetryService
{
    Task<HardwareTelemetrySnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class TelemetryService : ITelemetryService, IDisposable
{
    private static readonly TimeSpan HardwareRetryInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MotherboardInterval = TimeSpan.FromSeconds(3);

    private readonly object syncRoot = new();
    private readonly NetworkThroughputSampler networkSampler = new();

    private Computer? computer;
    private DateTimeOffset nextHardwareOpenAttempt;
    private DateTimeOffset nextMotherboardUpdate;
    private IReadOnlyList<TelemetrySensorReading> motherboardReadings =
        Array.Empty<TelemetrySensorReading>();
    private bool motherboardInventoryLogged;
    private bool disposed;

    public Task<HardwareTelemetrySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => CaptureSnapshot(cancellationToken), cancellationToken);
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CloseComputer();
        }
    }

    private HardwareTelemetrySnapshot CaptureSnapshot(CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            EnsureComputerIsOpen();
            var sensorReadings = ReadHardwareSensors();
            var memory = ReadMemoryStatus();
            var network = networkSampler.Sample();

            cancellationToken.ThrowIfCancellationRequested();

            return new HardwareTelemetrySnapshot(
                new TelemetrySummary(
                    CpuUsagePercent: TelemetrySensorSelector.SelectCpuLoad(sensorReadings),
                    CpuTemperatureCelsius: TelemetrySensorSelector.SelectCpuTemperature(sensorReadings),
                    MemoryUsedGb: memory.UsedGb,
                    MemoryTotalGb: memory.TotalGb,
                    NetworkUploadMbps: network.UploadMbps,
                    NetworkDownloadMbps: network.DownloadMbps),
                TelemetrySensorSelector.SelectGpu(sensorReadings),
                TelemetrySensorSelector.SelectAdvanced(sensorReadings, memory),
                TelemetrySensorSelector.SelectMotherboard(sensorReadings));
        }
    }

    private void EnsureComputerIsOpen()
    {
        if (computer is not null || DateTimeOffset.UtcNow < nextHardwareOpenAttempt)
        {
            return;
        }

        var candidate = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true
        };

        try
        {
            candidate.Open();
            computer = candidate;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hardware telemetry initialization failed: {ex.Message}");
            nextHardwareOpenAttempt = DateTimeOffset.UtcNow + HardwareRetryInterval;

            try
            {
                candidate.Close();
            }
            catch (Exception closeException)
            {
                Debug.WriteLine($"Hardware telemetry cleanup failed: {closeException.Message}");
            }
        }
    }

    private IReadOnlyList<TelemetrySensorReading> ReadHardwareSensors()
    {
        if (computer is null)
        {
            return Array.Empty<TelemetrySensorReading>();
        }

        var readings = new List<TelemetrySensorReading>();
        foreach (var hardware in computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Motherboard)
            {
                ReadMotherboard(hardware, readings);
            }
            else
            {
                UpdateHardware(hardware, readings);
            }
        }

        return readings;
    }

    private void ReadMotherboard(
        IHardware hardware,
        ICollection<TelemetrySensorReading> readings)
    {
        if (DateTimeOffset.UtcNow >= nextMotherboardUpdate)
        {
            var current = new List<TelemetrySensorReading>();
            UpdateHardware(hardware, current);
            motherboardReadings = current;
            nextMotherboardUpdate = DateTimeOffset.UtcNow + MotherboardInterval;

            if (!motherboardInventoryLogged)
            {
                motherboardInventoryLogged = true;
                AppLog.Write(
                    "motherboard.sensors",
                    current.Count == 0
                        ? "No supported motherboard sensors"
                        : string.Join(
                            " | ",
                            current
                                .OrderBy(reading => reading.HardwareName)
                                .ThenBy(reading => reading.SensorType)
                                .ThenBy(reading => reading.Name)
                                .Select(reading =>
                                    $"{reading.HardwareName}/{reading.Name}/" +
                                    $"{reading.SensorType}={reading.Value:F2}")));
            }
        }

        foreach (var reading in motherboardReadings)
        {
            readings.Add(reading);
        }
    }

    private static void UpdateHardware(
        IHardware hardware,
        ICollection<TelemetrySensorReading> readings)
    {
        try
        {
            hardware.Update();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ignoring telemetry update failure for {hardware.Name}: {ex.Message}");
        }

        if (hardware.HardwareType is
            HardwareType.Cpu or
            HardwareType.Memory or
            HardwareType.GpuNvidia or
            HardwareType.GpuAmd or
            HardwareType.GpuIntel or
            HardwareType.Motherboard or
            HardwareType.SuperIO or
            HardwareType.EmbeddedController)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.Value is not { } value || !float.IsFinite(value))
                {
                    continue;
                }

                readings.Add(new TelemetrySensorReading(
                    hardware.HardwareType,
                    hardware.Identifier.ToString(),
                    sensor.Name,
                    sensor.SensorType,
                    value,
                    hardware.Name));
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            UpdateHardware(subHardware, readings);
        }
    }

    private static MemorySummary ReadMemoryStatus()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        const double bytesPerGigabyte = 1024d * 1024d * 1024d;
        var total = status.TotalPhysical / bytesPerGigabyte;
        var available = status.AvailablePhysical / bytesPerGigabyte;
        var used = total - available;
        var virtualTotal = status.TotalPageFile / bytesPerGigabyte;
        var virtualUsed = (status.TotalPageFile - status.AvailablePageFile) / bytesPerGigabyte;
        return new MemorySummary(
            used,
            available,
            total,
            status.MemoryLoad,
            virtualUsed,
            virtualTotal);
    }

    private void CloseComputer()
    {
        if (computer is null)
        {
            return;
        }

        try
        {
            computer.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hardware telemetry shutdown failed: {ex.Message}");
        }
        finally
        {
            computer = null;
            motherboardReadings = Array.Empty<TelemetrySensorReading>();
            nextMotherboardUpdate = default;
            motherboardInventoryLogged = false;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

}
