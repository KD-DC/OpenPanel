using System.IO;

namespace OpenPanel.Host.Services;

public sealed class WebViewProfileService
{
    private static readonly string LocalStorageRelativePath = Path.Combine(
        "EBWebView",
        "Default",
        "Local Storage");

    private readonly string legacyUserDataFolder;

    public WebViewProfileService()
        : this(
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "OpenPanel",
                "WebView2"),
            (Environment.ProcessPath ??
                throw new InvalidOperationException(
                    "OpenPanel could not determine its executable path.")) +
                ".WebView2")
    {
    }

    internal WebViewProfileService(
        string userDataFolder,
        string legacyUserDataFolder)
    {
        UserDataFolder = Path.GetFullPath(userDataFolder);
        this.legacyUserDataFolder = Path.GetFullPath(legacyUserDataFolder);
    }

    public string UserDataFolder { get; }

    public bool MigrateLegacyLocalStorage()
    {
        var destination = Path.Combine(
            UserDataFolder,
            LocalStorageRelativePath);
        var source = Path.Combine(
            legacyUserDataFolder,
            LocalStorageRelativePath);
        var temporaryDestination = destination + ".migrating";

        try
        {
            if (ContainsFiles(destination) || !ContainsFiles(source))
            {
                return false;
            }

            if (Directory.Exists(temporaryDestination))
            {
                Directory.Delete(temporaryDestination, true);
            }
            CopyDirectory(source, temporaryDestination);

            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }
            Directory.Move(temporaryDestination, destination);
            AppLog.Write(
                "webview.profile.migrated",
                $"{legacyUserDataFolder} -> {UserDataFolder}");
            return true;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Write(
                "webview.profile.migration.failed",
                $"{ex.GetType().Name}: {ex.Message}");
            try
            {
                if (Directory.Exists(temporaryDestination))
                {
                    Directory.Delete(temporaryDestination, true);
                }
            }
            catch (Exception cleanupException) when (
                cleanupException is IOException or UnauthorizedAccessException)
            {
                AppLog.Write(
                    "webview.profile.migration.cleanup.failed",
                    $"{cleanupException.GetType().Name}: {cleanupException.Message}");
            }
            return false;
        }
    }

    private static bool ContainsFiles(string directory)
    {
        return Directory.Exists(directory) &&
            Directory.EnumerateFiles(
                directory,
                "*",
                SearchOption.AllDirectories).Any();
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.EnumerateDirectories(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(
                Path.Combine(destination, relativePath));
        }

        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            if (string.Equals(
                    Path.GetFileName(file),
                    "LOCK",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}
