using System.Windows.Forms;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public sealed class DisplayService
{
    private const int TargetWidth = 1920;
    private const int TargetHeight = 550;

    public DisplaySummary SelectDashboardDisplay()
    {
        var screens = Screen.AllScreens;
        var target = screens.FirstOrDefault(screen =>
            screen.Bounds.Width == TargetWidth &&
            screen.Bounds.Height == TargetHeight);

        target ??= Screen.PrimaryScreen ?? screens.First();

        return new DisplaySummary(
            target.DeviceName,
            target.Bounds.Left,
            target.Bounds.Top,
            target.Bounds.Width,
            target.Bounds.Height,
            target.Primary);
    }
}
