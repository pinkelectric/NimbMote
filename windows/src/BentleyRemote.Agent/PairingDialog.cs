namespace BentleyRemote.Agent;

internal sealed class PairingDialog : Form
{
    private readonly TextBox _code = new()
    {
        PlaceholderText = "6-digit code",
        MaxLength = 6,
        TextAlign = HorizontalAlignment.Center,
        Font = new Font(SystemFonts.DefaultFont.FontFamily, 18, FontStyle.Bold),
        Dock = DockStyle.Top
    };

    public PairingDialog()
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
            Text = "Open Bentley Remote on the phone and enter the six-digit code shown there. Pair only on your private hotspot.",
            Padding = new Padding(0, 0, 0, 10)
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44
        };
        var ok = new Button { Text = "Pair", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
        content.Controls.Add(_code);
        content.Controls.Add(explanation);
        content.Controls.Add(buttons);
        Controls.Add(content);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public string PairingCode => _code.Text.Trim();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && (PairingCode.Length != 6 || PairingCode.Any(character => !char.IsDigit(character))))
        {
            MessageBox.Show(this, "Enter exactly six digits.", "Bentley Remote", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }
}

