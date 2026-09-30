using System.Diagnostics;
using Forms = System.Windows.Forms;

namespace OpenPanel.Host;

internal sealed class StockConfigurationDialog : Forms.Form
{
    private readonly Forms.TextBox apiKeyTextBox;
    private readonly Forms.TextBox symbolsTextBox;

    public StockConfigurationDialog(string apiKey, IReadOnlyList<string> symbols)
    {
        Text = "Configure stock watchlist";
        StartPosition = Forms.FormStartPosition.CenterScreen;
        FormBorderStyle = Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new System.Drawing.Size(460, 238);
        Font = new System.Drawing.Font("Segoe UI", 10);

        var apiKeyLabel = new Forms.Label
        {
            AutoSize = true,
            Location = new System.Drawing.Point(20, 20),
            Text = "Twelve Data API key"
        };
        apiKeyTextBox = new Forms.TextBox
        {
            Location = new System.Drawing.Point(20, 46),
            Size = new System.Drawing.Size(420, 25),
            Text = apiKey,
            UseSystemPasswordChar = true
        };
        var keyLink = new Forms.LinkLabel
        {
            AutoSize = true,
            Location = new System.Drawing.Point(20, 78),
            Text = "Get a free personal API key"
        };
        keyLink.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(
            "https://twelvedata.com/pricing")
        {
            UseShellExecute = true
        });

        var symbolsLabel = new Forms.Label
        {
            AutoSize = true,
            Location = new System.Drawing.Point(20, 112),
            Text = "Symbols (comma separated, up to 8)"
        };
        symbolsTextBox = new Forms.TextBox
        {
            Location = new System.Drawing.Point(20, 138),
            Size = new System.Drawing.Size(420, 25),
            Text = string.Join(", ", symbols)
        };

        var cancelButton = new Forms.Button
        {
            DialogResult = Forms.DialogResult.Cancel,
            Location = new System.Drawing.Point(350, 187),
            Size = new System.Drawing.Size(90, 32),
            Text = "Cancel"
        };
        var saveButton = new Forms.Button
        {
            DialogResult = Forms.DialogResult.OK,
            Location = new System.Drawing.Point(250, 187),
            Size = new System.Drawing.Size(90, 32),
            Text = "Save"
        };

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.AddRange([
            apiKeyLabel,
            apiKeyTextBox,
            keyLink,
            symbolsLabel,
            symbolsTextBox,
            saveButton,
            cancelButton
        ]);
    }

    public string ApiKey => apiKeyTextBox.Text.Trim();

    public IReadOnlyList<string> Symbols => symbolsTextBox.Text
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
