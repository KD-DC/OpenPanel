import type {
  AirQualitySummary,
  DailyForecastSummary,
  HourlyForecastSummary,
  WeatherSummary
} from "../../types";

export function renderEnvironmentWidget(state: WeatherSummary): string {
  const today = state.daily[0];
  const condition = weatherCondition(state.weatherCode);

  return `
    <section class="environment-compact widget ${aqiLevel(state.airQuality.usAqi)}" aria-label="Weather">
      <header class="environment-compact__header">
        <span><i data-lucide="map-pin"></i>${escapeHtml(state.location)}</span>
        <button
          type="button"
          data-command="environment-expand"
          title="Expand weather"
          aria-label="Expand weather"
          aria-expanded="false">
          <i data-lucide="maximize-2"></i>
        </button>
      </header>
      <div class="environment-compact__current">
        <i data-lucide="${condition.icon}"></i>
        <div>
          <strong>${temperature(state.currentTemperatureFahrenheit)}</strong>
          <span>${condition.label}</span>
        </div>
      </div>
      <div class="environment-compact__high-low">
        <span>High <strong>${temperature(today?.highFahrenheit)}</strong></span>
        <span>Low <strong>${temperature(today?.lowFahrenheit)}</strong></span>
      </div>
      <div class="environment-compact__details">
        ${today?.precipitationProbabilityPercent
          ? `<span class="environment-compact__rain"><i data-lucide="umbrella"></i>Rain <strong>${Math.round(today.precipitationProbabilityPercent)}%</strong></span>`
          : ""}
        <span><i data-lucide="thermometer"></i>Feels like <strong>${temperature(state.apparentTemperatureFahrenheit)}</strong></span>
        <span><i data-lucide="droplets"></i>Humidity <strong>${number(state.humidityPercent, "%")}</strong></span>
        <span><i data-lucide="wind"></i>Wind <strong>${number(state.windSpeedMph, " mph")}</strong></span>
        <span class="environment-compact__aqi">
          <i data-lucide="leaf"></i>Air quality
          <strong>${state.airQuality.usAqi === null
            ? "--"
            : `${Math.round(state.airQuality.usAqi)} ${escapeHtml(state.airQuality.category)}`}</strong>
        </span>
      </div>
    </section>

    <section class="environment" aria-label="Weather and air quality">
      <div class="environment__current">
        <header class="environment__location">
          <span><i data-lucide="map-pin"></i>${escapeHtml(state.location)}</span>
          <div class="environment__header-actions">
            <small>${state.isStale ? "Cached" : state.status}</small>
            <button
              type="button"
              data-command="environment-expand"
              title="Collapse weather"
              aria-label="Collapse weather"
              aria-expanded="true">
              <i data-lucide="minimize-2"></i>
            </button>
          </div>
        </header>
        <div class="environment__current-main">
          <i data-lucide="${condition.icon}"></i>
          <div>
            <strong>${temperature(state.currentTemperatureFahrenheit)}</strong>
            <span>${condition.label}</span>
          </div>
        </div>
        <div class="environment__high-low">
          <span>High <strong>${temperature(today?.highFahrenheit)}</strong></span>
          <span>Low <strong>${temperature(today?.lowFahrenheit)}</strong></span>
        </div>
        <div class="environment__details">
          <span><i data-lucide="thermometer"></i>Feels like <strong>${temperature(state.apparentTemperatureFahrenheit)}</strong></span>
          <span><i data-lucide="droplets"></i>Humidity <strong>${number(state.humidityPercent, "%")}</strong></span>
          <span><i data-lucide="wind"></i>Wind <strong>${number(state.windSpeedMph, " mph")}</strong></span>
        </div>
      </div>

      <div class="environment__forecast">
        <header class="environment__section-head">
          <span>Hourly forecast</span>
          <small>Next 7 hours</small>
        </header>
        <div class="hourly-forecast">
          ${state.hourly.length > 0
            ? state.hourly.map(renderHour).join("")
            : `<span class="environment__empty">Forecast unavailable</span>`}
        </div>
        <div class="daily-forecast">
          ${state.daily.map(renderDay).join("")}
        </div>
      </div>

      ${renderAirQuality(state.airQuality)}
      <footer class="environment__attribution">
        Weather: Open-Meteo · Air quality: CAMS via Open-Meteo
      </footer>
    </section>
  `;
}

function renderHour(hour: HourlyForecastSummary, index: number): string {
  const condition = weatherCondition(hour.weatherCode);
  const time = new Date(hour.time).toLocaleTimeString([], { hour: "numeric" });
  return `
    <div class="hourly-forecast__item">
      <span>${index === 0 ? "Now" : time}</span>
      <i data-lucide="${condition.icon}" title="${condition.label}"></i>
      <strong>${temperature(hour.temperatureFahrenheit)}</strong>
      <small><i data-lucide="umbrella"></i>${number(hour.precipitationProbabilityPercent, "%")}</small>
    </div>
  `;
}

function renderDay(day: DailyForecastSummary, index: number): string {
  const condition = weatherCondition(day.weatherCode);
  const label = index === 0
    ? "Today"
    : new Date(`${day.date}T12:00:00`).toLocaleDateString([], { weekday: "short" });
  return `
    <div class="daily-forecast__item">
      <span class="daily-forecast__label">${label}</span>
      <i data-lucide="${condition.icon}" title="${condition.label}"></i>
      <strong>${temperature(day.highFahrenheit)}</strong>
      <small class="daily-forecast__low">${temperature(day.lowFahrenheit)}</small>
      <span class="daily-forecast__precipitation">
        <i data-lucide="umbrella"></i>${number(day.precipitationProbabilityPercent, "%")}
      </span>
    </div>
  `;
}

function renderAirQuality(air: AirQualitySummary): string {
  const level = aqiLevel(air.usAqi);
  return `
    <div class="environment__air ${level}">
      <header class="environment__section-head">
        <span><i data-lucide="leaf"></i>Air quality</span>
        <small>US AQI</small>
      </header>
      <div class="air-quality__primary">
        <strong>${air.usAqi === null ? "--" : Math.round(air.usAqi)}</strong>
        <span>${escapeHtml(air.category)}</span>
      </div>
      <div class="air-quality__scale" aria-label="Air quality index scale">
        <span style="inline-size:${aqiPosition(air.usAqi)}%"></span>
      </div>
      <div class="air-quality__pollutants">
        <span>PM2.5 <strong>${number(air.pm25, "")}</strong><small>µg/m³</small></span>
        <span>PM10 <strong>${number(air.pm10, "")}</strong><small>µg/m³</small></span>
        <span>Ozone <strong>${number(air.ozone, "")}</strong><small>µg/m³</small></span>
      </div>
    </div>
  `;
}

function weatherCondition(code: number | null): { icon: string; label: string } {
  if (code === null) return { icon: "cloud", label: "Unavailable" };
  if (code === 0) return { icon: "sun", label: "Clear" };
  if (code <= 2) return { icon: "cloud-sun", label: "Partly cloudy" };
  if (code === 3) return { icon: "cloud", label: "Overcast" };
  if (code === 45 || code === 48) return { icon: "cloud-fog", label: "Fog" };
  if (code >= 51 && code <= 67) return { icon: "cloud-rain", label: "Rain" };
  if (code >= 71 && code <= 77) return { icon: "cloud-snow", label: "Snow" };
  if (code >= 80 && code <= 82) return { icon: "cloud-rain", label: "Showers" };
  if (code >= 85 && code <= 86) return { icon: "cloud-snow", label: "Snow showers" };
  if (code >= 95) return { icon: "cloud-lightning", label: "Thunderstorms" };
  return { icon: "cloud", label: "Cloudy" };
}

function aqiLevel(aqi: number | null): string {
  if (aqi === null) return "aqi-unavailable";
  if (aqi <= 50) return "aqi-good";
  if (aqi <= 100) return "aqi-moderate";
  if (aqi <= 150) return "aqi-sensitive";
  return "aqi-unhealthy";
}

function aqiPosition(aqi: number | null): number {
  return aqi === null ? 0 : Math.max(2, Math.min(100, aqi / 3));
}

function temperature(value: number | null | undefined): string {
  return value === null || value === undefined ? "--" : `${Math.round(value)}°`;
}

function number(value: number | null | undefined, suffix: string): string {
  return value === null || value === undefined ? "--" : `${Math.round(value)}${suffix}`;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
