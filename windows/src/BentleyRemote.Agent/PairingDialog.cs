namespace BentleyRemote.Agent;

internal sealed class PairingDialog : Form
{
    private readonly System.Windows.Forms.Timer _pairingWatcher;

    public PairingDialog(string code, Func<bool> isPaired)
    {
        Text = "Pair Deskora";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(480, 252);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var explanation = new Label
        {
            AutoSize = false,
            Text = "Open Deskora on Android, enter this code and tap Search. The code expires after 10 minutes.",
            Padding = new Padding(0, 0, 0, 8),
            Dock = DockStyle.Fill,
        };
        var codeLabel = new Label
        {
            Text = code,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 18, FontStyle.Bold),
            Dock = DockStyle.Fill,
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
        };
        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            MinimumSize = new Size(88, 30),
        };
        buttons.Controls.Add(close);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 3,
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        content.Controls.Add(explanation, 0, 0);
        content.Controls.Add(codeLabel, 0, 1);
        content.Controls.Add(buttons, 0, 2);
        Controls.Add(content);
        AcceptButton = close;
        CancelButton = close;

        _pairingWatcher = new System.Windows.Forms.Timer { Interval = 250 };
        _pairingWatcher.Tick += (_, _) =>
        {
            if (!isPaired()) return;
            DialogResult = DialogResult.OK;
            Close();
        };
        Shown += (_, _) => _pairingWatcher.Start();
        FormClosed += (_, _) =>
        {
            _pairingWatcher.Stop();
            _pairingWatcher.Dispose();
        };
    }
}
