using System.Windows;
using OpenPanel.Host.Services;

namespace OpenPanel.Host;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Contains(
                NetworkProviderPermissionService.GrantArgument,
                StringComparer.Ordinal))
        {
            Shutdown(NetworkProviderPermissionService.GrantCurrentUserAccess());
            return;
        }

        base.OnStartup(e);
        new MainWindow().Show();
    }
}
