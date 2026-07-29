using System.IO;

namespace OpenPanel.Host.Services;

internal static class AppLog
{
    private static readonly object SyncRoot = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenPanel",
        "openpanel.log");

    public static void Write(string eventName, string detail)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(
                    LogPath,
                    $"{DateTimeOffset.Now:O}\t{eventName}\t{detail}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
            // Logging must never interrupt dashboard controls.
        }
    }
}
