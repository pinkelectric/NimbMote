using System.Net;
using System.Net.Sockets;

namespace BentleyRemote.Agent.Networking;

internal sealed class BootstrapDiscoveryListener : IAsyncDisposable
{
    private readonly Func<PairingWindow?> _getPairingWindow;
    private readonly Action<BootstrapCandidate> _candidateFound;
    private readonly Action<string>? _diagnostic;
    private readonly CancellationTokenSource _stop = new();
    private readonly HashSet<string> _seenNonces = new(StringComparer.Ordinal);
    private Task? _loop;

    public BootstrapDiscoveryListener(
        Func<PairingWindow?> getPairingWindow,
        Action<BootstrapCandidate> candidateFound,
        Action<string>? diagnostic)
    {
        _getPairingWindow = getPairingWindow;
        _candidateFound = candidateFound;
        _diagnostic = diagnostic;
    }

    public void Start() => _loop = Task.Run(() => RunAsync(_stop.Token));

    public void ResetReplayWindow()
    {
        lock (_seenNonces) _seenNonces.Clear();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, LanDiscovery.DiscoveryPort));
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult received;
            try { received = await udp.ReceiveAsync(cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (SocketException ex)
            {
                _diagnostic?.Invoke($"Bootstrap listener unavailable: {ex.SocketErrorCode}");
                break;
            }
            var window = _getPairingWindow();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (window is null || now > window.ExpiresAt) continue;
            BootstrapProbe probe;
            string reason;
            bool valid;
            lock (_seenNonces)
                valid = BootstrapDiscoveryProtocol.TryValidateProbe(
                    received.Buffer, window.Code, now, _seenNonces, out probe, out reason);
            if (!valid)
            {
                if (reason != "bootstrap code proof rejected") _diagnostic?.Invoke($"Bootstrap response rejected: {reason}");
                continue;
            }
            var response = BootstrapDiscoveryProtocol.CreateResponse(
                probe,
                window.AgentId,
                received.RemoteEndPoint.Address.ToString(),
                received.RemoteEndPoint.Port,
                window.Code);
            await udp.SendAsync(response, received.RemoteEndPoint, cancellationToken);
            _diagnostic?.Invoke($"Bootstrap candidate verified by pairing code: {received.RemoteEndPoint.Address}");
            _candidateFound(new BootstrapCandidate(received.RemoteEndPoint.Address, probe.PhoneId));
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        if (_loop is not null)
        {
            try { await _loop.WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
        }
        _stop.Dispose();
    }
}

internal sealed record PairingWindow(string AgentId, string Code, long ExpiresAt);
internal sealed record BootstrapCandidate(IPAddress Address, string PhoneId);
