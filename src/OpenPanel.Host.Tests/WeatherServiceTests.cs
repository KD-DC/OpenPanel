using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class WeatherServiceTests
{
    [TestMethod]
    public void NormalizeResponseSelectsSevenUpcomingHoursAndThreeDays()
    {
        var forecast = new WeatherService.ForecastResponse(
            new WeatherService.CurrentWeather(
                "2026-07-29T15:20",
                82,
                86,
                62,
                2,
                8),
            new WeatherService.HourlyWeather(
                [
                    "2026-07-29T14:00",
                    "2026-07-29T15:00",
                    "2026-07-29T16:00",
                    "2026-07-29T17:00",
                    "2026-07-29T18:00",
                    "2026-07-29T19:00",
                    "2026-07-29T20:00",
                    "2026-07-29T21:00",
                    "2026-07-29T22:00"
                ],
                [84, 83, 82, 81, 80, 79, 78, 77, 76],
                [1, 1, 2, 2, 3, 3, 61, 61, 2],
                [5, 8, 10, 15, 25, 35, 50, 42, 30]),
            new WeatherService.DailyWeather(
                ["2026-07-29", "2026-07-30", "2026-07-31", "2026-08-01"],
                [88, 86, 83, 85],
                [72, 70, 68, 69],
                [2, 61, 3, 1],
                [20, 75, 35, 10]));
        var air = new WeatherService.AirQualityResponse(
            new WeatherService.CurrentAirQuality(47, 8.2, 14.5, 63));

        var result = WeatherService.NormalizeResponse(
            forecast,
            air,
            DateTimeOffset.Parse("2026-07-29T15:21:00-04:00"),
            false);

        Assert.AreEqual(7, result.Hourly.Count);
        Assert.AreEqual(new DateTime(2026, 7, 29, 15, 0, 0), result.Hourly[0].Time);
        Assert.AreEqual(3, result.Daily.Count);
        Assert.AreEqual(88, result.Daily[0].HighFahrenheit);
        Assert.AreEqual(75, result.Daily[1].PrecipitationProbabilityPercent);
        Assert.AreEqual("Good", result.AirQuality.Category);
        Assert.AreEqual(8.2, result.AirQuality.Pm25);
    }

    [DataTestMethod]
    [DataRow(25, "Good")]
    [DataRow(75, "Moderate")]
    [DataRow(125, "Sensitive groups")]
    [DataRow(175, "Unhealthy")]
    [DataRow(250, "Very unhealthy")]
    [DataRow(350, "Hazardous")]
    public void DescribeAqiUsesUsEpaBands(double value, string expected)
    {
        Assert.AreEqual(expected, WeatherService.DescribeAqi(value));
    }
}
