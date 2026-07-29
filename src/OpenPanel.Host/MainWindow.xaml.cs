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
using Forms = System.Windows.Forms;

namespace OpenPanel.Host;

public partial class MainWindow : Window
{
    private static readonly TimeSpan TelemetryInterval = TimeSpan.FromSeconds(1);

    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoZOrder = 0x0004;

    private readonly DisplayService displayService = new();
    private readonly DashboardStateProvider stateProvider = new();
    private readonly SettingsService settingsService = new();
    private readonly TelemetryService telemetryService = new();
    private readonly AudioDeviceService audioDeviceService = new();
    private readonly MediaSessionService mediaSessionService = new();
    private readonly WeatherService weatherService;
    private readonly CancellationTokenSource telemetryCancellation = new();
    private readonly Forms.ContextMenuStrip trayMenu;
    private readonly Forms.ToolStripMenuItem currentAppearanceMenuItem;
    private readonly Forms.ToolStripMenuItem mediaOledAppearanceMenuItem;
    private readonly System.Drawing.Icon trayIconImage;
    private readonly Forms.NotifyIcon trayIcon;

    private DisplaySummary? selectedDisplay;
    private Task? telemetryLoopTask;

    public MainWindow()
    {
        InitializeComponent();
        weatherService = new WeatherService(settingsService.WeatherLocation);

        trayMenu = new Forms.ContextMenuStrip();
        trayMenu.Items.Add("Open OpenPanel", null, OnTrayOpen);

        var appearanceMenu = new Forms.ToolStripMenuItem("Appearance");
        currentAppearanceMenuItem = new Forms.ToolStripMenuItem(
            "Current",
            null,
            OnCurrentAppearance);
        mediaOledAppearanceMenuItem = new Forms.ToolStripMenuItem(
            "Media OLED",
            null,
            OnMediaOledAppearance);
        appearanceMenu.DropDownItems.Add(currentAppearanceMenuItem);
        appearanceMenu.DropDownItems.Add(mediaOledAppearanceMenuItem);
        trayMenu.Items.Add(appearanceMenu);
        trayMenu.Items.Add(new Forms.ToolStripSeparator());
        trayMenu.Items.Add("Exit OpenPanel", null, OnTrayExit);
        UpdateAppearanceMenu();

        trayIconImage = CreateTrayIcon();
        trayIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Icon = trayIconImage,
            Text = "OpenPanel",
            Visible = true
        };
        trayIcon.DoubleClick += OnTrayOpen;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        selectedDisplay = ApplyDashboardPlacement();
        AppLog.Write(
            "app.loaded",
            $"{selectedDisplay.Name}; {selectedDisplay.Width}x{selectedDisplay.Height}");

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
        trayIcon.Visible = false;
        trayIcon.DoubleClick -= OnTrayOpen;
        trayIcon.Dispose();
        trayIconImage.Dispose();
        trayMenu.Dispose();

        if (DashboardWebView.CoreWebView2 is { } coreWebView)
        {
            coreWebView.WebMessageReceived -= OnWebMessageReceived;
            coreWebView.NavigationCompleted -= OnNavigationCompleted;
        }

        telemetryService.Dispose();
        base.OnClosed(e);
    }

    private void OnTrayOpen(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Show();
            Activate();
        });
    }

    private void OnTrayExit(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() => System.Windows.Application.Current.Shutdown());
    }

    private async void OnCurrentAppearance(object? sender, EventArgs e)
    {
        await SetAppearanceAsync(SettingsService.CurrentAppearance);
    }

    private async void OnMediaOledAppearance(object? sender, EventArgs e)
    {
        await SetAppearanceAsync(SettingsService.MediaOledAppearance);
    }

    private async Task SetAppearanceAsync(string appearance)
    {
        try
        {
            await settingsService.SetAppearanceAsync(
                appearance,
                telemetryCancellation.Token);
            UpdateAppearanceMenu();
            AppLog.Write("appearance.changed", appearance);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Write(
                "appearance.failed",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void UpdateAppearanceMenu()
    {
        currentAppearanceMenuItem.Checked =
            settingsService.Appearance == SettingsService.CurrentAppearance;
        mediaOledAppearanceMenuItem.Checked =
            settingsService.Appearance == SettingsService.MediaOledAppearance;
    }

    private static System.Drawing.Icon CreateTrayIcon()
    {
        using var bitmap = new System.Drawing.Bitmap(
            32,
            32,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(System.Drawing.Color.Transparent);

        using var screenFill = new System.Drawing.SolidBrush(
            System.Drawing.Color.FromArgb(235, 5, 7, 10));
        using var accentPen = new System.Drawing.Pen(
            System.Drawing.Color.FromArgb(46, 211, 198),
            2.5f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        using var pulsePen = new System.Drawing.Pen(
            System.Drawing.Color.White,
            2.25f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };

        graphics.FillRectangle(screenFill, 3, 4, 26, 19);
        graphics.DrawRectangle(accentPen, 3.5f, 4.5f, 25, 18);
        graphics.DrawLines(
            pulsePen,
            [
                new System.Drawing.PointF(6, 14),
                new System.Drawing.PointF(10, 14),
                new System.Drawing.PointF(12.5f, 10),
                new System.Drawing.PointF(15.5f, 18),
                new System.Drawing.PointF(18.5f, 12),
                new System.Drawing.PointF(22, 12),
                new System.Drawing.PointF(24, 9),
                new System.Drawing.PointF(26, 9)
            ]);
        graphics.DrawLine(accentPen, 16, 23, 16, 27);
        graphics.DrawLine(accentPen, 11, 27, 21, 27);

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var icon = System.Drawing.Icon.FromHandle(iconHandle);
            return (System.Drawing.Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr iconHandle);

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
            AppLog.Write("dashboard.navigation.failed", e.WebErrorStatus.ToString());
            Debug.WriteLine($"Dashboard navigation failed: {e.WebErrorStatus}");
            return;
        }

        AppLog.Write("dashboard.navigation.completed", DashboardWebView.Source?.ToString() ?? "");
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
            case "command:audio.expanded":
                var expanded = DeserializePayload<AudioExpandedPayload>(command);
                audioDeviceService.SetExtendedState(expanded.IsExpanded);
                break;
            case "command:audio.input.select":
                var inputSelect = DeserializePayload<AudioInputSelectPayload>(command);
                await audioDeviceService.SelectInputAsync(
                    inputSelect.InputId,
                    cancellationToken);
                break;
            case "command:audio.input.volume":
                var inputVolume = DeserializePayload<AudioInputVolumePayload>(command);
                await audioDeviceService.SetInputVolumeAsync(
                    inputVolume.VolumePercent,
                    cancellationToken);
                break;
            case "command:audio.input.mute":
                var inputMute = DeserializePayload<AudioInputMutePayload>(command);
                await audioDeviceService.SetInputMutedAsync(
                    inputMute.IsMuted,
                    cancellationToken);
                break;
            case "command:audio.session.volume":
                var sessionVolume = DeserializePayload<AudioSessionVolumePayload>(command);
                await audioDeviceService.SetSessionVolumeAsync(
                    sessionVolume.SessionId,
                    sessionVolume.VolumePercent,
                    cancellationToken);
                break;
            case "command:audio.session.mute":
                var sessionMute = DeserializePayload<AudioSessionMutePayload>(command);
                await audioDeviceService.SetSessionMutedAsync(
                    sessionMute.SessionId,
                    sessionMute.IsMuted,
                    cancellationToken);
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
            case "command:media.shuffle":
                var shuffle = DeserializePayload<MediaShufflePayload>(command);
                await mediaSessionService.SetShuffleAsync(
                    shuffle.IsActive,
                    cancellationToken);
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
                    var weatherTask = weatherService.GetSnapshotAsync(cancellationToken);

                    await Task.WhenAll(
                        telemetryTask,
                        mediaTask,
                        audioTask,
                        weatherTask);
                    PostStateUpdate(
                        telemetryTask.Result,
                        mediaTask.Result,
                        audioTask.Result,
                        weatherTask.Result);
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
        AudioSummary audio,
        WeatherSummary weather)
    {
        var coreWebView = DashboardWebView.CoreWebView2;
        if (coreWebView is null || selectedDisplay is null)
        {
            return;
        }

        var state = stateProvider.CreateState(
            telemetry,
            media,
            audio,
            weather,
            settingsService.Appearance,
            selectedDisplay);
        var message = new HostToUiMessage("state:update", state);
        var json = JsonSerializer.Serialize(message, MessageJson.Options);
        coreWebView.PostWebMessageAsJson(json);
    }
}
