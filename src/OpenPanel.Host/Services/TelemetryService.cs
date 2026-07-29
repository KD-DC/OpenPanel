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

    private readonly object syncRoot = new();
    private readonly NetworkThroughputSampler networkSampler = new();

    private Computer? computer;
    private DateTimeOffset nextHardwareOpenAttempt;
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
                TelemetrySensorSelector.SelectGpu(sensorReadings));
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
            IsGpuEnabled = true
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
            UpdateHardware(hardware, readings);
        }

        return readings;
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
            HardwareType.GpuNvidia or
            HardwareType.GpuAmd or
            HardwareType.GpuIntel)
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
                    value));
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            UpdateHardware(subHardware, readings);
        }
    }

    private static MemoryStatus ReadMemoryStatus()
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
        var used = (status.TotalPhysical - status.AvailablePhysical) / bytesPerGigabyte;
        return new MemoryStatus(used, total);
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

    private readonly record struct MemoryStatus(double UsedGb, double TotalGb);
}
