using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenPanel.Host.Models;

namespace OpenPanel.Host.Services;

public interface IWeatherService
{
    Task<WeatherSummary> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class WeatherService : IWeatherService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(5);
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private readonly WeatherLocationSettings location;
    private readonly HttpClient httpClient;
    private readonly TimeProvider timeProvider;

    private WeatherSummary? cached;
    private DateTimeOffset nextRefresh = DateTimeOffset.MinValue;

    public WeatherService(
        WeatherLocationSettings location,
        HttpClient? httpClient = null,
        TimeProvider? timeProvider = null)
    {
        this.location = location;
        this.httpClient = httpClient ?? HttpClient;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<WeatherSummary> GetSnapshotAsync(
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (cached is not null && now < nextRefresh)
        {
            return cached;
        }

        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            now = timeProvider.GetUtcNow();
            if (cached is not null && now < nextRefresh)
            {
                return cached;
            }

            try
            {
                var weatherTask = GetJsonAsync<ForecastResponse>(
                    BuildForecastUri(),
                    cancellationToken);
                var airQualityTask = GetJsonAsync<AirQualityResponse>(
                    BuildAirQualityUri(),
                    cancellationToken);
                await Task.WhenAll(weatherTask, airQualityTask);

                cached = Normalize(
                    weatherTask.Result,
                    airQualityTask.Result,
                    now,
                    false);
                nextRefresh = now + RefreshInterval;
                AppLog.Write(
                    "weather.updated",
                    $"{location.Name}; aqi={cached.AirQuality.UsAqi?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}");
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                nextRefresh = now + RetryInterval;
                cached = cached is null
                    ? Unavailable("Weather service unavailable")
                    : cached with
                    {
                        IsStale = true,
                        Status = "Last update retained"
                    };
                AppLog.Write(
                    "weather.failed",
                    $"{ex.GetType().Name}: {ex.Message}");
            }

            return cached;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    internal static WeatherSummary NormalizeResponse(
        ForecastResponse forecast,
        AirQualityResponse airQuality,
        DateTimeOffset updatedAt,
        bool isStale)
    {
        var hourly = NormalizeHourly(forecast);
        var daily = NormalizeDaily(forecast);
        var aqi = airQuality.Current?.UsAqi;

        return new WeatherSummary(
            "",
            forecast.Current is not null,
            isStale,
            forecast.Current is null ? "Weather data unavailable" : "Updated",
            forecast.Current?.Temperature,
            forecast.Current?.ApparentTemperature,
            forecast.Current?.RelativeHumidity,
            forecast.Current?.WindSpeed,
            forecast.Current?.WeatherCode,
            hourly,
            daily,
            new AirQualitySummary(
                aqi,
                DescribeAqi(aqi),
                airQuality.Current?.Pm25,
                airQuality.Current?.Pm10,
                airQuality.Current?.Ozone),
            updatedAt);
    }

    private WeatherSummary Normalize(
        ForecastResponse forecast,
        AirQualityResponse airQuality,
        DateTimeOffset updatedAt,
        bool isStale)
    {
        return NormalizeResponse(forecast, airQuality, updatedAt, isStale) with
        {
            Location = location.Name
        };
    }

    internal static string DescribeAqi(double? aqi)
    {
        return aqi switch
        {
            null => "Unavailable",
            <= 50 => "Good",
            <= 100 => "Moderate",
            <= 150 => "Sensitive groups",
            <= 200 => "Unhealthy",
            <= 300 => "Very unhealthy",
            _ => "Hazardous"
        };
    }

    private async Task<T> GetJsonAsync<T>(
        Uri uri,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(
                   stream,
                   JsonOptions,
                   cancellationToken) ??
            throw new JsonException("Weather service returned an empty response.");
    }

    private Uri BuildForecastUri()
    {
        var coordinates = Coordinates();
        return new Uri(
            $"https://api.open-meteo.com/v1/forecast?{coordinates}" +
            "&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m" +
            "&hourly=temperature_2m,weather_code,precipitation_probability" +
            "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
            "&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=auto&forecast_days=3");
    }

    private Uri BuildAirQualityUri()
    {
        return new Uri(
            $"https://air-quality-api.open-meteo.com/v1/air-quality?{Coordinates()}" +
            "&current=us_aqi,pm2_5,pm10,ozone&timezone=auto&forecast_days=1");
    }

    private string Coordinates()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"latitude={location.Latitude}&longitude={location.Longitude}");
    }

    private WeatherSummary Unavailable(string status)
    {
        return new WeatherSummary(
            location.Name,
            false,
            false,
            status,
            null,
            null,
            null,
            null,
            null,
            Array.Empty<HourlyForecastSummary>(),
            Array.Empty<DailyForecastSummary>(),
            new AirQualitySummary(null, "Unavailable", null, null, null),
            null);
    }

    private static IReadOnlyList<HourlyForecastSummary> NormalizeHourly(
        ForecastResponse forecast)
    {
        var hourly = forecast.Hourly;
        if (hourly?.Time is null || forecast.Current?.Time is null)
        {
            return Array.Empty<HourlyForecastSummary>();
        }

        if (!DateTime.TryParse(
                forecast.Current.Time,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var currentTime))
        {
            return Array.Empty<HourlyForecastSummary>();
        }

        var currentHour = new DateTime(
            currentTime.Year,
            currentTime.Month,
            currentTime.Day,
            currentTime.Hour,
            0,
            0,
            currentTime.Kind);
        var result = new List<HourlyForecastSummary>(7);
        for (var index = 0; index < hourly.Time.Length && result.Count < 7; index++)
        {
            if (!DateTime.TryParse(
                    hourly.Time[index],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var time) ||
                time < currentHour)
            {
                continue;
            }

            result.Add(new HourlyForecastSummary(
                time,
                ValueAt(hourly.Temperature, index),
                ValueAt(hourly.WeatherCode, index),
                ValueAt(hourly.PrecipitationProbability, index)));
        }

        return result;
    }

    private static IReadOnlyList<DailyForecastSummary> NormalizeDaily(
        ForecastResponse forecast)
    {
        var daily = forecast.Daily;
        if (daily?.Time is null)
        {
            return Array.Empty<DailyForecastSummary>();
        }

        var count = Math.Min(3, daily.Time.Length);
        var result = new List<DailyForecastSummary>(count);
        for (var index = 0; index < count; index++)
        {
            if (!DateOnly.TryParse(
                    daily.Time[index],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                continue;
            }

            result.Add(new DailyForecastSummary(
                date,
                ValueAt(daily.TemperatureMax, index),
                ValueAt(daily.TemperatureMin, index),
                ValueAt(daily.WeatherCode, index),
                ValueAt(daily.PrecipitationProbabilityMax, index)));
        }

        return result;
    }

    private static T? ValueAt<T>(T?[]? values, int index)
        where T : struct
    {
        return values is not null && index < values.Length
            ? values[index]
            : null;
    }

    internal sealed record ForecastResponse(
        [property: JsonPropertyName("current")] CurrentWeather? Current,
        [property: JsonPropertyName("hourly")] HourlyWeather? Hourly,
        [property: JsonPropertyName("daily")] DailyWeather? Daily);

    internal sealed record CurrentWeather(
        [property: JsonPropertyName("time")] string? Time,
        [property: JsonPropertyName("temperature_2m")] double? Temperature,
        [property: JsonPropertyName("apparent_temperature")] double? ApparentTemperature,
        [property: JsonPropertyName("relative_humidity_2m")] double? RelativeHumidity,
        [property: JsonPropertyName("weather_code")] int? WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] double? WindSpeed);

    internal sealed record HourlyWeather(
        [property: JsonPropertyName("time")] string[]? Time,
        [property: JsonPropertyName("temperature_2m")] double?[]? Temperature,
        [property: JsonPropertyName("weather_code")] int?[]? WeatherCode,
        [property: JsonPropertyName("precipitation_probability")] double?[]?
            PrecipitationProbability);

    internal sealed record DailyWeather(
        [property: JsonPropertyName("time")] string[]? Time,
        [property: JsonPropertyName("temperature_2m_max")] double?[]? TemperatureMax,
        [property: JsonPropertyName("temperature_2m_min")] double?[]? TemperatureMin,
        [property: JsonPropertyName("weather_code")] int?[]? WeatherCode,
        [property: JsonPropertyName("precipitation_probability_max")] double?[]?
            PrecipitationProbabilityMax);

    internal sealed record AirQualityResponse(
        [property: JsonPropertyName("current")] CurrentAirQuality? Current);

    internal sealed record CurrentAirQuality(
        [property: JsonPropertyName("us_aqi")] double? UsAqi,
        [property: JsonPropertyName("pm2_5")] double? Pm25,
        [property: JsonPropertyName("pm10")] double? Pm10,
        [property: JsonPropertyName("ozone")] double? Ozone);
}
