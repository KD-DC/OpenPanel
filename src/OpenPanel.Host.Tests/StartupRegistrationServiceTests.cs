using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class StartupRegistrationServiceTests
{
    [TestMethod]
    public void MissingRegistrationIsDisabled()
    {
        var store = new FakeStartupRegistrationStore();
        var service = new StartupRegistrationService(
            store,
            @"C:\Apps\OpenPanel\OpenPanel.Host.exe");

        Assert.IsFalse(service.IsEnabled);
    }

    [TestMethod]
    public void EnablingWritesQuotedExecutablePath()
    {
        var store = new FakeStartupRegistrationStore();
        var service = new StartupRegistrationService(
            store,
            @"C:\Apps\Open Panel\OpenPanel.Host.exe");

        service.SetEnabled(true);

        Assert.AreEqual(
            "\"C:\\Apps\\Open Panel\\OpenPanel.Host.exe\"",
            store.Command);
        Assert.IsTrue(service.IsEnabled);
    }

    [TestMethod]
    public void DisablingRemovesRegistration()
    {
        var store = new FakeStartupRegistrationStore
        {
            Command = "\"C:\\Apps\\OpenPanel\\OpenPanel.Host.exe\""
        };
        var service = new StartupRegistrationService(
            store,
            @"C:\Apps\OpenPanel\OpenPanel.Host.exe");

        service.SetEnabled(false);

        Assert.IsNull(store.Command);
        Assert.IsFalse(service.IsEnabled);
    }

    [TestMethod]
    public void InstalledApplicationIsPreferredForStartup()
    {
        var resolved = StartupRegistrationService.ResolveStartupExecutablePath(
            @"C:\Dev\OpenPanel\bin\Debug\OpenPanel.Host.exe",
            @"C:\Users\Test\AppData\Local",
            path => path.EndsWith(
                @"Programs\OpenPanel\OpenPanel.Host.exe",
                StringComparison.Ordinal));

        Assert.AreEqual(
            @"C:\Users\Test\AppData\Local\Programs\OpenPanel\OpenPanel.Host.exe",
            resolved);
    }

    [TestMethod]
    public void CurrentApplicationIsUsedWithoutAnInstallation()
    {
        const string currentPath =
            @"C:\Portable\OpenPanel\OpenPanel.Host.exe";

        var resolved = StartupRegistrationService.ResolveStartupExecutablePath(
            currentPath,
            @"C:\Users\Test\AppData\Local",
            _ => false);

        Assert.AreEqual(currentPath, resolved);
    }

    private sealed class FakeStartupRegistrationStore :
        IStartupRegistrationStore
    {
        public string? Command { get; set; }

        public string? Read() => Command;

        public void Write(string command)
        {
            Command = command;
        }

        public void Delete()
        {
            Command = null;
        }
    }
}
