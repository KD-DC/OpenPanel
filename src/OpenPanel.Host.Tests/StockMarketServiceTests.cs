using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class StockMarketServiceTests
{
    [TestMethod]
    public void BatchQuotesAreNormalizedInConfiguredOrder()
    {
        var quotes = StockMarketService.ParseQuotes(
            """
            {
              "NVDA": {
                "symbol": "NVDA",
                "name": "NVIDIA Corporation",
                "close": "187.42",
                "change": "4.01",
                "percent_change": "2.18",
                "open": "184.00",
                "high": "188.00",
                "low": "182.00",
                "previous_close": "183.41",
                "is_market_open": true
              },
              "AMD": {
                "symbol": "AMD",
                "name": "Advanced Micro Devices Inc",
                "close": "164.09",
                "percent_change": "-0.63",
                "is_market_open": true
              }
            }
            """,
            ["AMD", "NVDA"]);

        Assert.HasCount(2, quotes);
        Assert.AreEqual("AMD", quotes[0].Symbol);
        Assert.AreEqual(-0.63, quotes[0].ChangePercent);
        Assert.AreEqual(187.42, quotes[1].Price);
        Assert.IsTrue(quotes[1].IsMarketOpen);
    }
}
