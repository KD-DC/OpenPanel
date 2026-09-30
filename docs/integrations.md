# Calendar and Stock Integrations

Both integrations are optional, personal-use features. They use direct HTTPS
requests from the .NET host and add no runtime packages, background servers, or
browser processes after setup.

## Google Tasks / Calendar

1. [Create or select a project in Google Cloud Console](https://console.cloud.google.com/projectselector2/home/dashboard).
2. [Enable the Google Calendar API](https://console.cloud.google.com/apis/library/calendar-json.googleapis.com)
   in that same project. The API page should say **Enabled** or show a
   **Manage** button.
3. [Enable the Google Tasks API](https://console.cloud.google.com/apis/library/tasks.googleapis.com)
   in the same project.
4. Configure the app name and support email under
   [Google Auth Platform > Branding](https://console.cloud.google.com/auth/branding).
5. Under [Audience](https://console.cloud.google.com/auth/audience), choose
   External and add your Google account as a test user while the app is in
   Testing mode.
6. Under [Data Access](https://console.cloud.google.com/auth/scopes), add
   `calendar.events.readonly`, `calendar.calendarlist.readonly`, and
   `tasks.readonly`.
7. Under [Clients](https://console.cloud.google.com/auth/clients), create an
   OAuth client with application type **Desktop app**.
8. Download its JSON credentials file.
9. Right-click the OpenPanel tray icon and choose
   **Integrations > Connect Google Tasks / Calendar**.
10. Select the downloaded JSON file and complete consent in the system browser.
11. Select the calendars OpenPanel should combine into the Today view. All
    incomplete Google Tasks due today are included automatically.

The project selected at the top of every Google Cloud page must match the
`project_id` in the downloaded JSON file. Enabling Calendar API in a different
project will leave calendar requests failing with HTTP 403.

OpenPanel requests only
`https://www.googleapis.com/auth/calendar.events.readonly` and
`https://www.googleapis.com/auth/calendar.calendarlist.readonly`, plus
`https://www.googleapis.com/auth/tasks.readonly`. Calendar selection can be
changed later from **Integrations > Select calendars**. Google Tasks only
provides a due date, not a due time, so OpenPanel places today's incomplete
tasks after all-day calendar events and before timed events. Completed and
undated tasks are not shown.
Personal-use apps with fewer than 100 users can remain unverified,
although Google displays an unverified-app warning. Testing-mode authorizations
expire after seven days; setting the app's publishing status to In production
avoids that test-token expiry while retaining the personal unverified-app limit.

The refresh token and OAuth client configuration are encrypted with Windows
DPAPI for the current user. Disconnecting from the tray deletes OpenPanel's
local credentials. Access can also be revoked from the Google Account security
page.

The downloaded JSON file is the desktop application's OAuth identity, not a
calendar export or a Google password. OpenPanel displays a guided setup screen
before the file picker. Importing it is required only once; normal connections
open Google's browser login and then OpenPanel's calendar checklist.

## Stock Watchlist

1. Create a personal Twelve Data account and API key.
2. Right-click the OpenPanel tray icon and choose
   **Integrations > Configure stock watchlist**.
3. Enter the API key and up to eight comma-separated symbols.

OpenPanel sends one batched quote request, caches the response, and derives the
small sparkline from locally accumulated quote samples. It retains up to 78
five-minute samples per symbol in
`%LOCALAPPDATA%\OpenPanel\Cache\stock-history.json`, covering a normal U.S.
trading day and surviving application restarts. The cache resets when the local
date changes. The API key and symbol list are encrypted with Windows DPAPI.
Data availability, delay, and licensing remain subject to the user's Twelve
Data plan.

## Resource Behavior

- Tasks and Calendar refresh at most every five minutes.
- Stocks refresh at most every five minutes while markets are open and every 30
  minutes while closed.
- Failed requests back off for one minute while retaining the last valid data.
- Disabling a widget from the tray prevents that integration from making
  network requests.
