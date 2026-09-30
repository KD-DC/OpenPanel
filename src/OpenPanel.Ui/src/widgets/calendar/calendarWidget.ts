import type { CalendarEventSummary, CalendarSummary } from "../../types";

export function renderCalendarWidget(state: CalendarSummary): string {
  const now = new Date();
  const allDayEvents = state.events.filter(event => event.isAllDay && !event.isTask);
  const taskEvents = state.events.filter(event => event.isTask);
  const timedEvents = state.events.filter(event => !event.isAllDay && !event.isTask);
  const upcoming = timedEvents.find(event => new Date(event.end) >= now);
  const dateLabel = now.toLocaleDateString([], { weekday: "long" });
  const monthLabel = now.toLocaleDateString([], { month: "long", year: "numeric" });
  const compactDate = now.toLocaleDateString([], { weekday: "long", month: "long", day: "numeric" });

  return `
    <button
      class="integration-resize"
      type="button"
      data-command="calendar-expand"
      title="Expand tasks and calendar"
      aria-label="Expand tasks and calendar"
      aria-expanded="false">
      <i class="integration-resize__expand" data-lucide="maximize-2"></i>
      <i class="integration-resize__collapse" data-lucide="minimize-2"></i>
    </button>
    <section class="integration-compact calendar-compact widget" aria-label="Google Tasks and Calendar">
      <header class="integration-compact__header calendar-compact__header">
        <span><i data-lucide="calendar-days"></i>Calendar</span>
        <small>${compactDate}</small>
      </header>
      ${state.isConnected
        ? renderCompactAgenda(state)
        : renderSetup("Connect Google Tasks / Calendar from the system tray")}
    </section>
    <section class="integration-expanded calendar-expanded" aria-label="Today's Google Tasks and Calendar">
      <aside class="calendar-expanded__date">
        <header class="integration-title"><i data-lucide="calendar-days"></i>Calendar</header>
        <div class="calendar-expanded__date-lockup">
          <strong>${now.getDate()}</strong>
          <span>${dateLabel}</span>
          <small>${monthLabel}</small>
        </div>
        <div class="calendar-expanded__groups">
          ${renderExpandedGroup("All day", allDayEvents, "sun")}
          ${renderExpandedGroup("Tasks due", taskEvents, "list-checks")}
        </div>
      </aside>
      <div class="calendar-expanded__agenda">
        <header><div><span>Timed schedule</span><strong>${timedEvents.length} event${timedEvents.length === 1 ? "" : "s"}</strong></div><small>${updatedAt(state.updatedAt)}</small></header>
        <div class="calendar-agenda">
          ${timedEvents.length > 0
            ? timedEvents.map((event, index) => renderAgendaEvent(event, index, now)).join("")
            : renderSetup(state.isConnected ? "No timed events today" : "Connect Google Tasks / Calendar from the system tray")}
        </div>
      </div>
      <aside class="calendar-expanded__next">
        <header class="integration-title"><i data-lucide="clock-3"></i>Up next</header>
        ${upcoming
          ? `<strong>${formatTime(upcoming)}</strong><h2>${escapeHtml(upcoming.title)}</h2><p>${escapeHtml(upcoming.location || duration(upcoming))}</p><span>${relativeStart(upcoming.start)}</span>`
          : `<div class="integration-empty">No more events today</div>`}
      </aside>
    </section>`;
}

function renderCompactAgenda(state: CalendarSummary): string {
  if (state.events.length === 0) {
    return renderSetup("Your agenda is clear today");
  }

  const allDay = state.events.filter(event => event.isAllDay && !event.isTask);
  const tasks = state.events.filter(event => event.isTask);
  const timed = state.events.filter(event => !event.isAllDay && !event.isTask);
  const upcomingTimed = timed.filter(event => new Date(event.end) >= new Date());
  const groupCount = Number(allDay.length > 0) + Number(tasks.length > 0);
  const upcomingLimit = 7 - groupCount;
  const visibleUpcoming = upcomingTimed.slice(0, upcomingLimit);

  return `
    <div class="calendar-compact__groups">
      ${renderCompactGroup("All day", allDay, "sun")}
      ${renderCompactGroup("Tasks due", tasks, "list-checks")}
    </div>
    ${visibleUpcoming.length > 0
      ? `<div class="calendar-compact__section-label">Up next</div>
         <div class="calendar-compact__events">${visibleUpcoming.map((event, index) => renderCompactEvent(event, index)).join("")}</div>`
      : `<div class="calendar-compact__empty">No more timed events today</div>`}`;
}

function renderCompactEvent(event: CalendarEventSummary, index: number): string {
  const detail = event.location || duration(event);
  return `
    <article class="calendar-compact__event" style="--event-color:${eventColor(index + 2)}">
      <i class="calendar-compact__mark"></i>
      <span><strong>${escapeHtml(event.title)}</strong><small>${escapeHtml(detail)}</small></span>
      <time>${formatTime(event)}</time>
    </article>`;
}

function renderCompactGroup(
  label: string,
  events: CalendarEventSummary[],
  icon: string
): string {
  if (events.length === 0) return "";
  return `
    <button class="calendar-compact__group" type="button" data-command="calendar-expand" aria-label="Expand ${label.toLowerCase()}">
      <i class="calendar-compact__group-icon" data-lucide="${icon}"></i>
      <span><small>${label} · ${events.length}</small><strong>${events.map(event => escapeHtml(event.title)).join(" · ")}</strong></span>
      <i class="calendar-compact__group-chevron" data-lucide="chevron-right"></i>
    </button>`;
}

function renderExpandedGroup(
  label: string,
  events: CalendarEventSummary[],
  icon: string
): string {
  if (events.length === 0) return "";
  return `
    <section class="calendar-expanded__group">
      <header><span><i data-lucide="${icon}"></i>${label}</span><strong>${events.length}</strong></header>
      ${events.map(event => `<div><i></i><span>${escapeHtml(event.title)}</span></div>`).join("")}
    </section>`;
}

function renderAgendaEvent(event: CalendarEventSummary, index: number, now: Date): string {
  return `
    <article class="calendar-agenda__event ${new Date(event.end) < now ? "is-past" : ""}" style="--event-color:${eventColor(index + 2)}">
      <time>${formatTime(event)}</time>
      <span></span>
      <div><strong>${escapeHtml(event.title)}</strong><small>${escapeHtml(event.location || duration(event))}</small></div>
      <em>${duration(event)}</em>
    </article>`;
}

function renderSetup(message: string): string {
  return `<div class="integration-setup"><i data-lucide="calendar-clock"></i><strong>${escapeHtml(message)}</strong></div>`;
}

function formatTime(event: CalendarEventSummary): string {
  return event.isAllDay
    ? "All day"
    : new Date(event.start).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
}

function duration(event: CalendarEventSummary): string {
  if (event.isTask) return "Due today";
  if (event.isAllDay) return "All day";
  const minutes = Math.max(0, Math.round((new Date(event.end).getTime() - new Date(event.start).getTime()) / 60000));
  return minutes >= 60 && minutes % 60 === 0 ? `${minutes / 60} hr` : `${minutes} min`;
}

function relativeStart(value: string): string {
  const minutes = Math.round((new Date(value).getTime() - Date.now()) / 60000);
  if (minutes <= 0) return "Now";
  if (minutes < 60) return `in ${minutes} min`;
  return `in ${Math.round(minutes / 60)} hr`;
}

function updatedAt(value: string | null): string {
  return value ? `Updated ${new Date(value).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })}` : "";
}

function eventColor(index: number): string {
  return ["#48d8c4", "#f2ca5e", "#72b6ff", "#ec7bb9"][index % 4]!;
}

function escapeHtml(value: string): string {
  return value.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#039;");
}
