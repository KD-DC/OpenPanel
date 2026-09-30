using OpenPanel.Host.Services;
using Forms = System.Windows.Forms;

namespace OpenPanel.Host;

internal sealed class CalendarSelectionDialog : Forms.Form
{
    private readonly Forms.CheckedListBox calendarList;

    public CalendarSelectionDialog(IReadOnlyList<GoogleCalendarOption> calendars)
    {
        Text = "Select Google calendars";
        StartPosition = Forms.FormStartPosition.CenterScreen;
        FormBorderStyle = Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new System.Drawing.Size(460, 390);
        Font = new System.Drawing.Font("Segoe UI", 10);

        var heading = new Forms.Label
        {
            AutoSize = true,
            Location = new System.Drawing.Point(20, 18),
            Text = "Calendars shown in OpenPanel"
        };
        var description = new Forms.Label
        {
            AutoSize = false,
            Location = new System.Drawing.Point(20, 45),
            Size = new System.Drawing.Size(420, 42),
            Text = "Select one or more calendars. OpenPanel reads today's events only."
        };
        calendarList = new Forms.CheckedListBox
        {
            CheckOnClick = true,
            Location = new System.Drawing.Point(20, 92),
            Size = new System.Drawing.Size(420, 235),
            IntegralHeight = false
        };
        foreach (var calendar in calendars)
        {
            calendarList.Items.Add(
                new CalendarListItem(calendar.Id, calendar.Name, calendar.IsPrimary),
                calendar.IsSelected);
        }

        var cancelButton = new Forms.Button
        {
            DialogResult = Forms.DialogResult.Cancel,
            Location = new System.Drawing.Point(350, 342),
            Size = new System.Drawing.Size(90, 32),
            Text = "Cancel"
        };
        var saveButton = new Forms.Button
        {
            Location = new System.Drawing.Point(250, 342),
            Size = new System.Drawing.Size(90, 32),
            Text = "Save"
        };
        saveButton.Click += (_, _) =>
        {
            if (calendarList.CheckedItems.Count == 0)
            {
                Forms.MessageBox.Show(
                    "Select at least one calendar.",
                    "OpenPanel",
                    Forms.MessageBoxButtons.OK,
                    Forms.MessageBoxIcon.Information);
                return;
            }
            DialogResult = Forms.DialogResult.OK;
            Close();
        };

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.AddRange([
            heading,
            description,
            calendarList,
            saveButton,
            cancelButton
        ]);
    }

    public IReadOnlyList<string> SelectedCalendarIds => calendarList.CheckedItems
        .Cast<CalendarListItem>()
        .Select(item => item.Id)
        .ToArray();

    private sealed record CalendarListItem(
        string Id,
        string Name,
        bool IsPrimary)
    {
        public override string ToString() => IsPrimary ? $"{Name} (Primary)" : Name;
    }
}
