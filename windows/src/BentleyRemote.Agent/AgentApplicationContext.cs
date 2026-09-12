using BentleyRemote.Agent.Core;
using BentleyRemote.Agent.Diagnostics;
using BentleyRemote.Agent.Installation;
using System.Reflection;

namespace BentleyRemote.Agent;

internal sealed class AgentApplicationContext : ApplicationContext, IDisposable
{
    private readonly AgentCoordinator _coordinator = new();
    private readonly NotifyIcon _trayIcon;
    private readonly Icon _applicationIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _pairItem;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly bool _showPairingOnStart;
    private bool _initialPairingShown;
    private bool _disposing;

    public AgentApplicationContext(bool showPairingOnStart = false)
    {
        _showPairingOnStart = showPairingOnStart;
        _statusItem = new ToolStripMenuItem("Starting…") { Enabled = false };
        _pairItem = new ToolStripMenuItem("Pair with phone…", null, PairClicked);
        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _statusItem,
            new ToolStripSeparator(),
            _pairItem,
            new ToolStripMenuItem("About / diagnostics…", null, AboutClicked),
            new ToolStripMenuItem("Copy connection diagnostics", null, CopyDiagnosticsClicked),
            new ToolStripMenuItem("Copy hotspot gateways", null, CopyGatewaysClicked),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, ExitClicked)
        });
        _applicationIcon = DeskoraTrayIcon.Create();
        _trayIcon = new NotifyIcon
        {
            Icon = _applicationIcon,
            Text = "NimbMote",
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

        if (_showPairingOnStart && !_initialPairingShown && !_coordinator.IsPaired)
        {
            _initialPairingShown = true;
            var pairing = _coordinator.BeginPairing();
            using var dialog = new PairingDialog(pairing.Code, () => _coordinator.IsPaired);
            dialog.ShowDialog();
        }
    }

    private async void PairClicked(object? sender, EventArgs e)
    {
        if (_coordinator.IsPaired)
        {
            var answer = MessageBox.Show(
                "This removes the existing pairing before accepting a new code. Continue?",
                "NimbMote",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            await _coordinator.ForgetPhoneAsync();
        }
        var pairing = _coordinator.BeginPairing();
        using var dialog = new PairingDialog(pairing.Code, () => _coordinator.IsPaired);
        dialog.ShowDialog();
    }

    private static void CopyGatewaysClicked(object? sender, EventArgs e)
    {
        var gateways = Networking.GatewayDiscovery.FindIPv4Gateways();
        var text = gateways.Count == 0 ? "No IPv4 gateway detected" : string.Join(Environment.NewLine, gateways);
        Clipboard.SetText(text);
    }

    private static void CopyDiagnosticsClicked(object? sender, EventArgs e) =>
        Clipboard.SetText(DiagnosticsJournal.Read());

    private static void AboutClicked(object? sender, EventArgs e)
    {
        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly?.GetName().Version?.ToString() ?? "unknown";
        MessageBox.Show(
            $"NimbMote Agent\nVersion: {version}\nExecutable: {Environment.ProcessPath}\n\n" +
            $"Installed path: {AgentInstallLayout.ExecutablePath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))}",
            "NimbMote diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _applicationIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
