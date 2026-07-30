using System.IO;
using Microsoft.Win32;

namespace OpenPanel.Host.Services;

public interface IStartupRegistrationService
{
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
}

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    private readonly IStartupRegistrationStore store;
    private readonly string executablePath;

    public StartupRegistrationService()
        : this(
            new RegistryStartupRegistrationStore(),
            ResolveStartupExecutablePath(
                Environment.ProcessPath ??
                    throw new InvalidOperationException(
                        "OpenPanel could not determine its executable path."),
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                File.Exists))
    {
    }

    internal StartupRegistrationService(
        IStartupRegistrationStore store,
        string executablePath)
    {
        this.store = store;
        this.executablePath = Path.GetFullPath(executablePath);
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(store.Read());

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            store.Write($"\"{executablePath}\"");
            return;
        }

        store.Delete();
    }

    internal static string ResolveStartupExecutablePath(
        string currentExecutablePath,
        string localApplicationData,
        Func<string, bool> fileExists)
    {
        var installedExecutablePath = Path.Combine(
            localApplicationData,
            "Programs",
            "OpenPanel",
            "OpenPanel.Host.exe");
        return fileExists(installedExecutablePath)
            ? installedExecutablePath
            : currentExecutablePath;
    }
}

internal interface IStartupRegistrationStore
{
    string? Read();
    void Write(string command);
    void Delete();
}

internal sealed class RegistryStartupRegistrationStore :
    IStartupRegistrationStore
{
    private const string RunKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "OpenPanel";

    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(
            ValueName,
            null,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
    }

    public void Write(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    public void Delete()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(ValueName, false);
    }
}
