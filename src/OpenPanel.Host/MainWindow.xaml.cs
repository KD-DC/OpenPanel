using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using OpenPanel.Host.Messaging;
using OpenPanel.Host.Models;
using OpenPanel.Host.Services;

namespace OpenPanel.Host;

public partial class MainWindow : Window
{
    private static readonly TimeSpan TelemetryInterval = TimeSpan.FromSeconds(1);

    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoZOrder = 0x0004;

    private readonly DisplayService displayService = new();
    private readonly DashboardStateProvider stateProvider = new();
    private readonly TelemetryService telemetryService = new();
    private readonly AudioDeviceService audioDeviceService = new();
    private readonly MediaSessionService mediaSessionService = new();
    private readonly CancellationTokenSource telemetryCancellation = new();

    private DisplaySummary? selectedDisplay;
    private Task? telemetryLoopTask;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        selectedDisplay = ApplyDashboardPlacement();

        try
        {
            await DashboardWebView.EnsureCoreWebView2Async();
            DashboardWebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(5, 7, 10);

            var coreWebView = DashboardWebView.CoreWebView2;
            coreWebView.Settings.AreDefaultContextMenusEnabled = false;
#if DEBUG
            coreWebView.Settings.AreDevToolsEnabled = true;
#else
            coreWebView.Settings.AreDevToolsEnabled = false;
#endif
            coreWebView.WebMessageReceived += OnWebMessageReceived;
            coreWebView.NavigationCompleted += OnNavigationCompleted;

            coreWebView.SetVirtualHostNameToFolderMapping(
                "openpanel.local",
                ResolveDashboardDirectory(),
                CoreWebView2HostResourceAccessKind.DenyCors);
            DashboardWebView.Source = new Uri("https://openpanel.local/index.html");
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            System.Windows.MessageBox.Show(
                "OpenPanel requires the Microsoft Edge WebView2 Evergreen Runtime.",
                "OpenPanel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Debug.WriteLine(ex);
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        telemetryCancellation.Cancel();

        if (DashboardWebView.CoreWebView2 is { } coreWebView)
        {
            coreWebView.WebMessageReceived -= OnWebMessageReceived;
            coreWebView.NavigationCompleted -= OnNavigationCompleted;
        }

        telemetryService.Dispose();
        base.OnClosed(e);
    }

    private DisplaySummary ApplyDashboardPlacement()
    {
        var target = displayService.SelectDashboardDisplay();
        WindowState = WindowState.Normal;

        var windowHandle = new WindowInteropHelper(this).Handle;
        if (!SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                target.Left,
                target.Top,
                target.Width,
                target.Height,
                SwpNoActivate | SwpNoZOrder))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        Activate();
        return target;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    private static string ResolveDashboardDirectory()
    {
        var dashboardDirectory = Path.Combine(AppContext.BaseDirectory, "Dashboard");
        var indexPath = Path.Combine(dashboardDirectory, "index.html");

        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException(
                "The dashboard bundle was not found. Run the UI build before starting OpenPanel.",
                indexPath);
        }

        return dashboardDirectory;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            Debug.WriteLine($"Dashboard navigation failed: {e.WebErrorStatus}");
            return;
        }

        telemetryLoopTask ??= RunTelemetryLoopAsync(telemetryCancellation.Token);
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var command = JsonSerializer.Deserialize<UiToHostMessage>(e.WebMessageAsJson, MessageJson.Options);
            if (command is null)
            {
                return;
            }

            AppLog.Write("command.received", command.Type);
            await ExecuteCommandAsync(command, telemetryCancellation.Token);
            AppLog.Write("command.completed", command.Type);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Write("command.failed", $"{ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"Ignoring failed UI command: {ex.Message}");
        }
    }

    private async Task ExecuteCommandAsync(
        UiToHostMessage command,
        CancellationToken cancellationToken)
    {
        switch (command.Type)
        {
            case "command:audio.select":
                var select = DeserializePayload<AudioSelectPayload>(command);
                AppLog.Write(
                    "audio.select",
                    $"{select.OutputId}; communications={select.SetCommunicationsDevice}");
                await audioDeviceService.SelectOutputAsync(
                    select.OutputId,
                    select.SetCommunicationsDevice,
                    cancellationToken);
                break;
            case "command:audio.volume":
                var volume = DeserializePayload<AudioVolumePayload>(command);
                await audioDeviceService.SetVolumeAsync(
                    volume.VolumePercent,
                    cancellationToken);
                break;
            case "command:audio.mute":
                var mute = DeserializePayload<AudioMutePayload>(command);
                await audioDeviceService.SetMutedAsync(mute.IsMuted, cancellationToken);
                break;
            case "command:media.toggle":
                await mediaSessionService.TogglePlayPauseAsync(cancellationToken);
                break;
            case "command:media.previous":
                await mediaSessionService.GoPreviousAsync(cancellationToken);
                break;
            case "command:media.next":
                await mediaSessionService.GoNextAsync(cancellationToken);
                break;
            case "command:media.seek":
                var seek = DeserializePayload<MediaSeekPayload>(command);
                await mediaSessionService.SeekAsync(seek.PositionSeconds, cancellationToken);
                break;
            case "command:system.ready":
                break;
            default:
                Debug.WriteLine($"Ignoring unknown UI command: {command.Type}");
                break;
        }
    }

    private static TPayload DeserializePayload<TPayload>(UiToHostMessage command)
    {
        if (command.Payload is not { } payload)
        {
            throw new JsonException($"Command {command.Type} requires a payload.");
        }

        return payload.Deserialize<TPayload>(MessageJson.Options) ??
            throw new JsonException($"Command {command.Type} has an invalid payload.");
    }

    private async Task RunTelemetryLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TelemetryInterval);

        try
        {
            do
            {
                try
                {
                    var telemetryTask = telemetryService.GetSnapshotAsync(cancellationToken);
                    var mediaTask = mediaSessionService.GetCurrentSessionAsync(cancellationToken);
                    var audioTask = audioDeviceService.GetOutputsAsync(cancellationToken);

                    await Task.WhenAll(telemetryTask, mediaTask, audioTask);
                    PostStateUpdate(
                        telemetryTask.Result,
                        mediaTask.Result,
                        audioTask.Result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Debug.WriteLine($"Telemetry snapshot failed: {ex.Message}");
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Window shutdown cancels the loop.
        }
    }

    private void PostStateUpdate(
        HardwareTelemetrySnapshot telemetry,
        MediaSummary media,
        AudioSummary audio)
    {
        var coreWebView = DashboardWebView.CoreWebView2;
        if (coreWebView is null || selectedDisplay is null)
        {
            return;
        }

        var state = stateProvider.CreateState(telemetry, media, audio, selectedDisplay);
        var message = new HostToUiMessage("state:update", state);
        var json = JsonSerializer.Serialize(message, MessageJson.Options);
        coreWebView.PostWebMessageAsJson(json);
    }
}
