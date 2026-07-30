using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace OpenPanel.Host.Services;

internal static class NetworkProviderPermissionService
{
    internal const string GrantArgument = "--grant-network-etw";

    private static readonly Guid NetworkProviderId =
        new("7dd42a49-5329-4832-8dfd-43d979153a88");
    private const uint EventSecurityAddDacl = 2;
    private const uint TraceLogGuidEnable = 0x0080;
    private const int ErrorCancelled = 1223;

    public static async Task<bool> RequestGrantAsync(CancellationToken cancellationToken)
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            AppLog.Write("network.permission.failed", "Executable path unavailable");
            return false;
        }

        try
        {
            AppLog.Write(
                "network.permission.requested",
                $"exe={executablePath}");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = GrantArgument,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = AppContext.BaseDirectory
            });
            if (process is null)
            {
                AppLog.Write("network.permission.failed", "Elevated helper did not start");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            var succeeded = process.ExitCode == 0;
            AppLog.Write(
                succeeded ? "network.permission.granted" : "network.permission.failed",
                $"exit={process.ExitCode}");
            return succeeded;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == ErrorCancelled)
        {
            AppLog.Write("network.permission.cancelled", exception.Message);
            return false;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            AppLog.Write(
                "network.permission.failed",
                $"{exception.GetType().Name}: {exception.Message}");
            return false;
        }
    }

    public static int GrantCurrentUserAccess()
    {
        try
        {
            var sid = WindowsIdentity.GetCurrent().User;
            if (sid is null)
            {
                return 1;
            }

            var sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);
            var sidPointer = Marshal.AllocHGlobal(sidBytes.Length);
            try
            {
                Marshal.Copy(sidBytes, 0, sidPointer, sidBytes.Length);
                var providerId = NetworkProviderId;
                var result = EventAccessControl(
                    ref providerId,
                    EventSecurityAddDacl,
                    sidPointer,
                    TraceLogGuidEnable,
                    true);
                AppLog.Write("network.permission.helper", $"result={result}");
                return result == 0 ? 0 : unchecked((int)result);
            }
            finally
            {
                Marshal.FreeHGlobal(sidPointer);
            }
        }
        catch (Exception)
        {
            return 1;
        }
    }

    [DllImport("sechost.dll", CharSet = CharSet.Unicode)]
    private static extern uint EventAccessControl(
        ref Guid guid,
        uint operation,
        nint sid,
        uint rights,
        [MarshalAs(UnmanagedType.Bool)] bool allowOrDeny);
}
