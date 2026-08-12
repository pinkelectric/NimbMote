using BentleyRemote.Agent.Core;
using Microsoft.Win32;

namespace BentleyRemote.Agent;

internal sealed class AgentApplicationContext : ApplicationContext, IDisposable
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "Bentley Remote";
    private readonly AgentCoordinator _coordinator = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _pairItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _disposing;

    public AgentApplicationContext()
    {
        _statusItem = new ToolStripMenuItem("Starting…") { Enabled = false };
        _pairItem = new ToolStripMenuItem("Pair with phone…", null, PairClicked);
        _autoStartItem = new ToolStripMenuItem("Start with Windows", null, AutoStartClicked)
        {
            Checked = IsAutoStartEnabled(),
            CheckOnClick = false
        };
        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _statusItem,
            new ToolStripSeparator(),
            _pairItem,
            new ToolStripMenuItem("Copy hotspot gateways", null, CopyGatewaysClicked),
            _autoStartItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, ExitClicked)
        });
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Bentley Remote",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += PairClicked;

        _timer = new System.Windows.Forms.Timer { Interval = 750, Enabled = true };
        _timer.Tick += (_, _) => RefreshMenu();
        _ = _coordinator.StartAsync();
    }

    private void RefreshMenu()
    {
        var status = _coordinator.Status;
        _statusItem.Text = status.Length > 72 ? status[..69] + "…" : status;
        _trayIcon.Text = (_coordinator.IsConnected ? "Connected — " : "Disconnected — ") +
                         (status.Length > 45 ? status[..45] : status);
        _pairItem.Text = _coordinator.IsPaired
            ? $"Re-pair {_coordinator.PairedPhoneName ?? "phone"}…"
            : "Pair with phone…";
    }

    private async void PairClicked(object? sender, EventArgs e)
    {
        if (_coordinator.IsPaired)
        {
            var answer = MessageBox.Show(
                "This removes the existing pairing before accepting a new code. Continue?",
                "Bentley Remote",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            await _coordinator.ForgetPhoneAsync();
        }
        var pairing = _coordinator.BeginPairing();
        using var dialog = new PairingDialog(pairing.Code);
        dialog.ShowDialog();
    }

    private static void CopyGatewaysClicked(object? sender, EventArgs e)
    {
        var gateways = Networking.GatewayDiscovery.FindIPv4Gateways();
        var text = gateways.Count == 0 ? "No IPv4 gateway detected" : string.Join(Environment.NewLine, gateways);
        Clipboard.SetText(text);
    }

    private void AutoStartClicked(object? sender, EventArgs e)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (_autoStartItem.Checked)
        {
            key.DeleteValue(RunValueName, false);
            _autoStartItem.Checked = false;
        }
        else
        {
            key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
            _autoStartItem.Checked = true;
        }
    }

    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is string;
    }

    private async void ExitClicked(object? sender, EventArgs e)
    {
        if (_disposing) return;
        _disposing = true;
        _timer.Stop();
        _trayIcon.Visible = false;
        await _coordinator.DisposeAsync();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
