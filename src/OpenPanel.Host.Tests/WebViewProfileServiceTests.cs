using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class WebViewProfileServiceTests
{
    private string? temporaryDirectory;

    [TestCleanup]
    public void Cleanup()
    {
        if (temporaryDirectory is not null &&
            Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, true);
        }
    }

    [TestMethod]
    public void LegacyLocalStorageMigratesWithoutBrowserCacheOrLock()
    {
        var root = CreateTemporaryDirectory();
        var shared = Path.Combine(root, "shared");
        var legacy = Path.Combine(root, "legacy");
        var legacyStorage = LocalStoragePath(legacy);
        Directory.CreateDirectory(Path.Combine(legacyStorage, "leveldb"));
        File.WriteAllText(
            Path.Combine(legacyStorage, "leveldb", "000003.log"),
            "openpanel.widget-layout.v3");
        File.WriteAllText(
            Path.Combine(legacyStorage, "leveldb", "LOCK"),
            "stale lock");
        Directory.CreateDirectory(Path.Combine(legacy, "EBWebView", "Cache"));
        File.WriteAllText(
            Path.Combine(legacy, "EBWebView", "Cache", "large.cache"),
            "cache");

        var service = new WebViewProfileService(shared, legacy);

        Assert.IsTrue(service.MigrateLegacyLocalStorage());
        Assert.IsTrue(File.Exists(Path.Combine(
            LocalStoragePath(shared),
            "leveldb",
            "000003.log")));
        Assert.IsFalse(File.Exists(Path.Combine(
            LocalStoragePath(shared),
            "leveldb",
            "LOCK")));
        Assert.IsFalse(Directory.Exists(Path.Combine(
            shared,
            "EBWebView",
            "Cache")));
    }

    [TestMethod]
    public void ExistingSharedLocalStorageIsNotOverwritten()
    {
        var root = CreateTemporaryDirectory();
        var shared = Path.Combine(root, "shared");
        var legacy = Path.Combine(root, "legacy");
        var sharedStorage = LocalStoragePath(shared);
        var legacyStorage = LocalStoragePath(legacy);
        Directory.CreateDirectory(sharedStorage);
        Directory.CreateDirectory(legacyStorage);
        File.WriteAllText(Path.Combine(sharedStorage, "layout.log"), "shared");
        File.WriteAllText(Path.Combine(legacyStorage, "layout.log"), "legacy");

        var service = new WebViewProfileService(shared, legacy);

        Assert.IsFalse(service.MigrateLegacyLocalStorage());
        Assert.AreEqual(
            "shared",
            File.ReadAllText(Path.Combine(sharedStorage, "layout.log")));
    }

    [TestMethod]
    public void MissingLegacyProfileRequiresNoMigration()
    {
        var root = CreateTemporaryDirectory();
        var service = new WebViewProfileService(
            Path.Combine(root, "shared"),
            Path.Combine(root, "missing"));

        Assert.IsFalse(service.MigrateLegacyLocalStorage());
    }

    private string CreateTemporaryDirectory()
    {
        temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"OpenPanel.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        return temporaryDirectory;
    }

    private static string LocalStoragePath(string userDataFolder)
    {
        return Path.Combine(
            userDataFolder,
            "EBWebView",
            "Default",
            "Local Storage");
    }
}
