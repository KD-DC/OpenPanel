using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class WidgetCatalogTests
{
    [TestMethod]
    public void CatalogHasUniqueStableIds()
    {
        var ids = WidgetCatalog.All.Select(widget => widget.Id).ToArray();

        Assert.AreEqual(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.IsTrue(ids.All(id => !string.IsNullOrWhiteSpace(id)));
    }

    [TestMethod]
    public void SummaryDefaultsEveryWidgetToVisible()
    {
        var summary = WidgetCatalog.CreateSummary(new HashSet<string>());

        Assert.AreEqual(WidgetCatalog.All.Count, summary.Items.Count);
        Assert.IsTrue(summary.Items.All(widget => widget.IsVisible));
    }
}
