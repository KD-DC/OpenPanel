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

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var command = JsonSerializer.Deserialize<UiToHostMessage>(e.WebMessageAsJson, MessageJson.Options);
            Debug.WriteLine($"UI command received: {command?.Type}");
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Ignoring invalid UI message: {ex.Message}");
        }
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
                    var snapshot = await telemetryService.GetSnapshotAsync(cancellationToken);
                    PostStateUpdate(snapshot);
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

    private void PostStateUpdate(HardwareTelemetrySnapshot telemetry)
    {
        var coreWebView = DashboardWebView.CoreWebView2;
        if (coreWebView is null || selectedDisplay is null)
        {
            return;
        }

        var state = stateProvider.CreateState(telemetry, selectedDisplay);
        var message = new HostToUiMessage("state:update", state);
        var json = JsonSerializer.Serialize(message, MessageJson.Options);
        coreWebView.PostWebMessageAsJson(json);
    }
}
