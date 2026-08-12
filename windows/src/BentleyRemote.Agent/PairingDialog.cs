namespace BentleyRemote.Agent;

internal sealed class PairingDialog : Form
{
    public PairingDialog(string code)
    {
        Text = "Pair Bentley Remote";
        Width = 390;
        Height = 230;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var explanation = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 74,
            Text = "Open Bentley Remote on Android, enter this code and tap Search. The code expires after 10 minutes.",
            Padding = new Padding(0, 0, 0, 10)
        };
        var codeLabel = new Label
        {
            Text = code,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 18, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 44
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44
        };
        var close = new Button { Text = "Close", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(close);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
        content.Controls.Add(codeLabel);
        content.Controls.Add(explanation);
        content.Controls.Add(buttons);
        Controls.Add(content);
        AcceptButton = close;
        CancelButton = close;
    }
}
