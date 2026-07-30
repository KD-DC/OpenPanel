using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public static class WidgetCatalog
{
    public static IReadOnlyList<WidgetDefinition> All { get; } =
    [
        new("system", "Hardware"),
        new("media", "Media"),
        new("audio", "Audio"),
        new("memory", "Memory"),
        new("cpu-power", "CPU performance"),
        new("gpu-power", "GPU performance"),
        new("gpu-thermals", "GPU thermals"),
        new("storage", "Storage"),
        new("environment", "Weather"),
        new("peripherals", "Peripheral batteries"),
        new("gaming", "Gaming performance")
    ];

    public static bool Contains(string widgetId)
    {
        return All.Any(widget =>
            string.Equals(widget.Id, widgetId, StringComparison.Ordinal));
    }

    public static WidgetConfigurationSummary CreateSummary(
        IReadOnlySet<string> disabledWidgets)
    {
        return new WidgetConfigurationSummary(
            All.Select(widget => new WidgetVisibilitySummary(
                    widget.Id,
                    widget.Label,
                    !disabledWidgets.Contains(widget.Id)))
                .ToArray());
    }
}

public sealed record WidgetDefinition(string Id, string Label);
