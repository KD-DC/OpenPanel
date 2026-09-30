using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace OpenPanel.Host.Services;

internal sealed class SecureIntegrationStore<T>(string fileName) where T : class
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenPanel",
        "Integrations",
        fileName);

    public T? Load()
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var protectedBytes = Convert.FromBase64String(File.ReadAllText(path));
            var json = Encoding.UTF8.GetString(WindowsDataProtection.Unprotect(protectedBytes));
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex) when (
            ex is IOException or JsonException or FormatException or Win32Exception)
        {
            AppLog.Write("integration.settings.read.failed", $"{fileName}; {ex.Message}");
            return null;
        }
    }

    public async Task SaveAsync(T value, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path) ??
            throw new InvalidOperationException("The integration settings path has no directory.");
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(value, JsonOptions);
        var protectedBytes = WindowsDataProtection.Protect(Encoding.UTF8.GetBytes(json));
        var temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(
            temporaryPath,
            Convert.ToBase64String(protectedBytes),
            cancellationToken);
        File.Move(temporaryPath, path, true);
    }

    public void Delete()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

internal static class WindowsDataProtection
{
    private const uint CryptProtectUiForbidden = 0x1;

    public static byte[] Protect(byte[] value)
    {
        return Transform(value, protect: true);
    }

    public static byte[] Unprotect(byte[] value)
    {
        return Transform(value, protect: false);
    }

    private static byte[] Transform(byte[] value, bool protect)
    {
        var input = CreateBlob(value);
        var output = new DataBlob();
        try
        {
            var succeeded = protect
                ? CryptProtectData(
                    ref input,
                    "OpenPanel integration settings",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    ref output)
                : CryptUnprotectData(
                    ref input,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    ref output);
            if (!succeeded)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, result.Length);
            return result;
        }
        finally
        {
            if (input.Data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(input.Data);
            }
            if (output.Data != IntPtr.Zero)
            {
                LocalFree(output.Data);
            }
        }
    }

    private static DataBlob CreateBlob(byte[] value)
    {
        var data = Marshal.AllocHGlobal(value.Length);
        Marshal.Copy(value, 0, data, value.Length);
        return new DataBlob(value.Length, data);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob(int size, IntPtr data)
    {
        public int Size = size;
        public IntPtr Data = data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn,
        string description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        uint flags,
        ref DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn,
        IntPtr description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        uint flags,
        ref DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
