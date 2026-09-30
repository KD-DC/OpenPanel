using System.Diagnostics;
using Forms = System.Windows.Forms;

namespace OpenPanel.Host;

internal sealed class GoogleCalendarSetupDialog : Forms.Form
{
    private const string ProjectDashboardUrl =
        "https://console.cloud.google.com/projectselector2/home/dashboard";
    private const string CalendarApiUrl =
        "https://console.cloud.google.com/apis/library/calendar-json.googleapis.com";
    private const string TasksApiUrl =
        "https://console.cloud.google.com/apis/library/tasks.googleapis.com";
    private const string BrandingUrl =
        "https://console.cloud.google.com/auth/branding";
    private const string AudienceUrl =
        "https://console.cloud.google.com/auth/audience";
    private const string DataAccessUrl =
        "https://console.cloud.google.com/auth/scopes";
    private const string ClientsUrl =
        "https://console.cloud.google.com/auth/clients";

    public GoogleCalendarSetupDialog()
    {
        Text = "Connect Google Tasks / Calendar";
        StartPosition = Forms.FormStartPosition.CenterScreen;
        FormBorderStyle = Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new System.Drawing.Size(840, 510);
        Font = new System.Drawing.Font("Segoe UI", 10);

        var heading = new Forms.Label
        {
            AutoSize = true,
            Font = new System.Drawing.Font("Segoe UI Semibold", 15),
            Location = new System.Drawing.Point(24, 18),
            Text = "One-time Google Tasks / Calendar setup"
        };
        var explanation = new Forms.Label
        {
            AutoSize = false,
            Location = new System.Drawing.Point(24, 54),
            Size = new System.Drawing.Size(792, 44),
            Text =
                "Complete these steps in one Google Cloud project. The project selected at the top of each Google page " +
                "must be the same project used to create the downloaded OAuth JSON file."
        };

        var steps = new Forms.TableLayoutPanel
        {
            AutoSize = false,
            ColumnCount = 2,
            Location = new System.Drawing.Point(24, 96),
            RowCount = 7,
            Size = new System.Drawing.Size(792, 307),
            CellBorderStyle = Forms.TableLayoutPanelCellBorderStyle.Single
        };
        steps.ColumnStyles.Add(new Forms.ColumnStyle(Forms.SizeType.Percent, 72));
        steps.ColumnStyles.Add(new Forms.ColumnStyle(Forms.SizeType.Percent, 28));
        steps.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Absolute, 38));
        steps.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Absolute, 38));
        steps.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Absolute, 38));
        steps.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Absolute, 38));
        steps.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Absolute, 42));
        steps.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Absolute, 70));
        steps.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Absolute, 38));

        AddStep(steps, 0,
            "1. Create or select the Google Cloud project that will own OpenPanel's OAuth client.",
            "Open project dashboard", ProjectDashboardUrl);
        AddStep(steps, 1,
            "2. Enable Google Calendar API. Confirm the page says Enabled or shows a Manage button.",
            "Enable Calendar API", CalendarApiUrl);
        AddStep(steps, 2,
            "3. Enable Google Tasks API in the same project.",
            "Enable Tasks API", TasksApiUrl);
        AddStep(steps, 3,
            "4. Configure the app name and support email under Google Auth Platform > Branding.",
            "Open Branding", BrandingUrl);
        AddStep(steps, 4,
            "5. Under Audience, choose External. In Testing mode, add your Google account as a test user.",
            "Open Audience", AudienceUrl);
        AddStep(steps, 5,
            "6. Under Data Access, add calendar.events.readonly, calendar.calendarlist.readonly, and tasks.readonly. " +
            "OpenPanel can only read events, task lists, and incomplete dated tasks.",
            "Open Data Access", DataAccessUrl);
        AddStep(steps, 6,
            "7. Under Clients, create a Desktop app OAuth client and download its JSON file.",
            "Open OAuth Clients", ClientsUrl);

        var nextStep = new Forms.Label
        {
            AutoSize = false,
            Location = new System.Drawing.Point(24, 408),
            Size = new System.Drawing.Size(792, 51),
            Text =
                "Next: choose the downloaded JSON file below. OpenPanel will open Google's sign-in and consent page, " +
                "then show a checklist of calendars. Dated incomplete tasks are included automatically. The JSON identifies the desktop app; it is not a calendar export " +
                "and does not contain your Google password."
        };
        var cancelButton = new Forms.Button
        {
            DialogResult = Forms.DialogResult.Cancel,
            Location = new System.Drawing.Point(704, 465),
            Size = new System.Drawing.Size(112, 34),
            Text = "Cancel"
        };
        var chooseButton = new Forms.Button
        {
            DialogResult = Forms.DialogResult.OK,
            Location = new System.Drawing.Point(536, 465),
            Size = new System.Drawing.Size(158, 34),
            Text = "Choose JSON file..."
        };

        AcceptButton = chooseButton;
        CancelButton = cancelButton;
        Controls.AddRange([
            heading,
            explanation,
            steps,
            nextStep,
            chooseButton,
            cancelButton
        ]);
    }

    private static void AddStep(
        Forms.TableLayoutPanel table,
        int row,
        string instruction,
        string linkText,
        string url)
    {
        var label = new Forms.Label
        {
            AutoSize = false,
            Dock = Forms.DockStyle.Fill,
            Margin = new Forms.Padding(12, 6, 8, 4),
            Text = instruction,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        var link = new Forms.LinkLabel
        {
            AutoSize = false,
            Dock = Forms.DockStyle.Fill,
            Margin = new Forms.Padding(12, 6, 8, 4),
            Text = linkText,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });

        table.Controls.Add(label, 0, row);
        table.Controls.Add(link, 1, row);
    }
}
