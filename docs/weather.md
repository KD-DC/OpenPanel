# Weather And Air Quality

OpenPanel uses Open-Meteo for forecast data and the Open-Meteo Air Quality API
for CAMS air-quality data. Neither endpoint requires an API key for this use.
No additional package dependency is used.

## Display

The third swipeable page shows:

- Current temperature, condition, feels-like temperature, humidity, and wind.
- Today's high and low.
- Seven upcoming hourly temperature, condition, and precipitation readings.
- Three daily forecasts with high and low temperatures.
- U.S. AQI category and current PM2.5, PM10, and ozone concentrations.

## Refresh Behavior

The WPF host fetches weather and air quality concurrently. A successful response
is cached for 15 minutes. Failed requests retain the last successful response and
wait five minutes before retrying. The TypeScript UI never contacts the APIs
directly.

## Location

The initial location is Washington, DC. To change it, close OpenPanel and edit:

`%LOCALAPPDATA%\OpenPanel\settings.json`

```json
{
  "appearance": "mediaOled",
  "weatherLocation": {
    "name": "Baltimore, MD",
    "latitude": 39.2904,
    "longitude": -76.6122
  }
}
```

Latitude must be between -90 and 90. Longitude must be between -180 and 180.
Invalid or missing location values fall back to Washington, DC.

## Attribution

Weather data is provided by Open-Meteo. Air-quality forecasts use CAMS data via
Open-Meteo. Attribution is displayed directly on the Environment page.

- https://open-meteo.com/en/docs
- https://open-meteo.com/en/docs/air-quality-api
