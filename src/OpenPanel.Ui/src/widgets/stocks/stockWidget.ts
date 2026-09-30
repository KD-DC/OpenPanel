import type { StockMarketSummary, StockQuoteSummary } from "../../types";

export function renderStockWidget(state: StockMarketSummary): string {
  const quotes = state.quotes.slice(0, 8);
  const averageChange = quotes.length > 0
    ? quotes.reduce((sum, quote) => sum + (quote.changePercent ?? 0), 0) / quotes.length
    : null;
  return `
    <button
      class="integration-resize"
      type="button"
      data-command="stocks-expand"
      title="Expand watchlist"
      aria-label="Expand watchlist"
      aria-expanded="false">
      <i class="integration-resize__expand" data-lucide="maximize-2"></i>
      <i class="integration-resize__collapse" data-lucide="minimize-2"></i>
    </button>
    <section class="integration-compact stocks-compact widget" aria-label="Stock watchlist">
      <header class="integration-compact__header"><span><i data-lucide="chart-no-axes-combined"></i>Watchlist</span></header>
      ${state.isConfigured
        ? `<div class="stocks-summary">
            <span><i class="${state.isMarketOpen ? "is-open" : ""}"></i>${escapeHtml(state.status)}</span>
            <strong class="${changeClass(averageChange)}">${percent(averageChange)}</strong>
          </div>
          <div class="stock-list">${quotes.slice(0, 4).map(renderCompactQuote).join("")}</div>`
        : `<div class="integration-setup"><i data-lucide="badge-dollar-sign"></i><strong>Configure stocks from the system tray</strong></div>`}
      <footer class="integration-footer"><span>${quotes.length} symbol${quotes.length === 1 ? "" : "s"}</span><span>${state.isStale ? "Cached" : updatedAt(state.updatedAt)}</span></footer>
    </section>
    <section class="integration-expanded stocks-expanded" aria-label="Expanded stock watchlist">
      <header class="stocks-expanded__header">
        <div><span class="integration-title"><i data-lucide="chart-no-axes-combined"></i>Market watchlist</span><strong>${escapeHtml(state.status)}</strong></div>
        <div><small>Watchlist today</small><strong class="${changeClass(averageChange)}">${percent(averageChange)}</strong></div>
      </header>
      <div class="stocks-expanded__grid" style="--stock-columns:${Math.max(1, Math.min(quotes.length, 6))}">
        ${quotes.length > 0 ? quotes.map(renderExpandedQuote).join("") : `<div class="integration-setup"><strong>${escapeHtml(state.status)}</strong></div>`}
      </div>
      <footer class="stocks-expanded__footer"><span>Quotes supplied by Twelve Data</span><span>${state.isStale ? "Showing cached data" : updatedAt(state.updatedAt)}</span></footer>
    </section>`;
}

function renderCompactQuote(quote: StockQuoteSummary): string {
  return `
    <article class="stock-row">
      <div><strong>${escapeHtml(quote.symbol)}</strong><small>${escapeHtml(shortName(quote.name))}</small></div>
      ${sparkline(quote)}
      <div><strong>${money(quote.price)}</strong><small class="${changeClass(quote.changePercent)}">${percent(quote.changePercent)}</small></div>
    </article>`;
}

function renderExpandedQuote(quote: StockQuoteSummary): string {
  return `
    <article class="stock-card">
      <header><div><strong>${escapeHtml(quote.symbol)}</strong><span>${escapeHtml(quote.name)}</span></div><small class="${changeClass(quote.changePercent)}">${percent(quote.changePercent)}</small></header>
      <div class="stock-card__price">${money(quote.price)} <small class="${changeClass(quote.change)}">${signedMoney(quote.change)}</small></div>
      ${sparkline(quote, true)}
      <footer><span>Open <strong>${money(quote.open)}</strong></span><span>High <strong>${money(quote.high)}</strong></span><span>Low <strong>${money(quote.low)}</strong></span><span>Prev <strong>${money(quote.previousClose)}</strong></span></footer>
    </article>`;
}

function sparkline(quote: StockQuoteSummary, large = false): string {
  const source = quote.history.length > 1
    ? quote.history
    : [quote.open ?? quote.previousClose ?? quote.price, quote.price];
  const width = large ? 300 : 74;
  const height = large ? 68 : 32;
  const min = Math.min(...source);
  const max = Math.max(...source);
  const range = Math.max(0.01, max - min);
  const points = source.map((value, index) => {
    const x = source.length === 1 ? width : index / (source.length - 1) * width;
    const y = height - 3 - (value - min) / range * (height - 6);
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(" ");
  return `<svg class="stock-spark ${changeClass(quote.changePercent)} ${large ? "stock-spark--large" : ""}" viewBox="0 0 ${width} ${height}" preserveAspectRatio="none" aria-hidden="true"><polyline points="${points}"></polyline></svg>`;
}

function shortName(value: string): string {
  return value.replace(/\b(incorporated|corporation|company|inc|corp)\.?\b/gi, "").trim();
}

function money(value: number | null): string {
  return value === null ? "--" : `$${value.toLocaleString([], { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function signedMoney(value: number | null): string {
  if (value === null) return "";
  return `${value >= 0 ? "+" : "−"}$${Math.abs(value).toFixed(2)}`;
}

function percent(value: number | null): string {
  if (value === null) return "--";
  return `${value >= 0 ? "+" : "−"}${Math.abs(value).toFixed(2)}%`;
}

function changeClass(value: number | null): string {
  if (value === null || value === 0) return "is-flat";
  return value > 0 ? "is-up" : "is-down";
}

function updatedAt(value: string | null): string {
  return value ? `Updated ${new Date(value).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })}` : "";
}

function escapeHtml(value: string): string {
  return value.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#039;");
}
