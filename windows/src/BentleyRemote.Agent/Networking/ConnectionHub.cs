using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using BentleyRemote.Agent.Protocol;
using BentleyRemote.Agent.Security;

namespace BentleyRemote.Agent.Networking;

internal sealed class ConnectionHub : IAsyncDisposable
{
    private const int AndroidPort = 45892;
    private const int ReversePort = 45893;
    private readonly AgentConfigStore _configStore;
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentDictionary<string, long> _seenNonces = new();
    private readonly ConcurrentQueue<BootstrapCandidate> _bootstrapCandidates = new();
    private readonly object _peerGate = new();
    private SocketPeer? _activePeer;
    private SocketPeer? _primaryPeer;
    private string? _pendingPairCode;
    private Task? _primaryTask;
    private Task? _reverseTask;
    private Task? _heartbeatTask;
    private BootstrapDiscoveryListener? _bootstrapListener;
    private PairingWindow? _pairingWindow;

    public ConnectionHub(AgentConfigStore configStore) => _configStore = configStore;

    public event Action<ProtocolMessage>? MessageReceived;
    public event Action<bool, string>? ConnectionChanged;

    public bool IsConnected
    {
        get { lock (_peerGate) return _activePeer is { IsOpen: true, IsAuthenticated: true }; }
    }

    public void Start()
    {
        _bootstrapListener = new BootstrapDiscoveryListener(
            () => _pairingWindow,
            candidate => _bootstrapCandidates.Enqueue(candidate),
            Diagnostic);
        _bootstrapListener.Start();
        _primaryTask = Task.Run(() => PrimaryReconnectLoopAsync(_stop.Token));
        _reverseTask = Task.Run(() => ReverseServerLoopAsync(_stop.Token));
        _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_stop.Token));
    }

    public PairingWindow BeginPairing()
    {
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var window = new PairingWindow(
            _configStore.Current.AgentId,
            code,
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds());
        _pendingPairCode = code;
        _pairingWindow = window;
        _bootstrapListener?.ResetReplayWindow();
        RaiseConnection(false, "Pairing window open; enter the code on Android");
        return window;
    }

    public async Task DisconnectAsync()
    {
        SocketPeer? peer;
        lock (_peerGate) peer = _activePeer;
        if (peer is not null)
        {
            try { await peer.CloseAsync(WebSocketCloseStatus.NormalClosure, "Pairing reset", _stop.Token); }
            catch (WebSocketException) { }
        }
    }

    public async Task SendAsync(string type, object payload, string? replyTo = null)
    {
        SocketPeer? peer;
        lock (_peerGate) peer = _activePeer;
        if (peer is not { IsAuthenticated: true, IsOpen: true }) return;
        await peer.SendAsync(ProtocolCodec.Serialize(type, payload, replyTo), _stop.Token);
    }

    private async Task PrimaryReconnectLoopAsync(CancellationToken cancellationToken)
    {
        var delaySeconds = 1;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (IsConnected)
            {
                await DelayAsync(1, cancellationToken);
                continue;
            }

            var candidates = new List<ConnectionCandidate>();
            var config = _configStore.Current;
            if (_pairingWindow is { } pairingWindow &&
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > pairingWindow.ExpiresAt)
            {
                _pairingWindow = null;
                _pendingPairCode = null;
                while (_bootstrapCandidates.TryDequeue(out _)) { }
                RaiseConnection(false, "Pairing window expired; open a new code from the tray");
            }
            while (_bootstrapCandidates.TryDequeue(out var bootstrapCandidate))
                candidates.Add(new ConnectionCandidate(
                    bootstrapCandidate.Address,
                    AndroidPort,
                    "bootstrap discovery",
                    bootstrapCandidate.PhoneId));
            if (config.IsPaired)
            {
                if (IPAddress.TryParse(config.LastKnownAddress, out var lastKnown) &&
                    lastKnown.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    candidates.Add(new ConnectionCandidate(lastKnown, AndroidPort, "last-known"));
                candidates.AddRange(GatewayDiscovery.FindIPv4Gateways()
                    .Select(address => new ConnectionCandidate(address, AndroidPort, "gateway")));
            }

            var attempted = new HashSet<string>(StringComparer.Ordinal);
            var heldSession = false;
            foreach (var candidate in candidates)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (!_configStore.Current.IsPaired && candidate.ExpectedPhoneId is null)
                {
                    Diagnostic($"{candidate.Path} endpoint {candidate.Address} skipped: bootstrap identity required");
                    continue;
                }
                if (!attempted.Add(candidate.Address.ToString())) continue;
                heldSession = await ConnectCandidateAsync(candidate, cancellationToken);
                if (heldSession || IsConnected) break;
            }

            if (!heldSession && !IsConnected && config.IsPaired && _configStore.GetSecret() is { } secret &&
                !string.IsNullOrWhiteSpace(config.PhoneId))
            {
                RaiseConnection(false, "Searching for paired phone on LAN…");
                var discovered = await LanDiscovery.DiscoverAsync(
                    config.AgentId,
                    config.PhoneId,
                    secret,
                    Diagnostic,
                    cancellationToken);
                foreach (var endpoint in discovered)
                {
                    if (!attempted.Add(endpoint.Address.ToString())) continue;
                    heldSession = await ConnectCandidateAsync(
                        new ConnectionCandidate(endpoint.Address, endpoint.Port, "LAN discovery"),
                        cancellationToken);
                    if (heldSession || IsConnected) break;
                }
            }

            if (heldSession)
            {
                delaySeconds = 1;
                continue;
            }
            RaiseDisconnectedStatus("Disconnected; reconnecting automatically");
            await DelayWithJitterAsync(delaySeconds, cancellationToken);
            delaySeconds = Math.Min(delaySeconds * 2, 10);
        }
    }

    private async Task<bool> ConnectCandidateAsync(
        ConnectionCandidate candidate,
        CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectTimeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            var uri = new Uri($"ws://{candidate.Address}:{candidate.Port}/bentley");
            RaiseConnection(false, $"Connecting via {candidate.Path}: {candidate.Address}…");
            await socket.ConnectAsync(uri, connectTimeout.Token);
            var peer = new SocketPeer(
                socket,
                $"Android {candidate.Path} {candidate.Address}",
                candidate.Address,
                candidate.Path,
                candidate.ExpectedPhoneId);
            lock (_peerGate) _primaryPeer = peer;
            await AuthenticateOrPairPrimaryAsync(peer, cancellationToken);

            var receiveTask = ReceiveLoopAsync(peer, allowIncomingAuthHello: false, cancellationToken);
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(6), cancellationToken));
            if (!peer.IsAuthenticated && peer.IsOpen)
            {
                Diagnostic($"{candidate.Path} endpoint {candidate.Address} rejected: WebSocket authentication timeout");
                await peer.CloseAsync(WebSocketCloseStatus.PolicyViolation, "authentication timeout", cancellationToken);
            }
            await receiveTask;
            return peer.WasAuthenticated;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Diagnostic($"{candidate.Path} endpoint {candidate.Address} rejected: connection timeout");
        }
        catch (WebSocketException ex)
        {
            Diagnostic($"{candidate.Path} endpoint {candidate.Address} rejected: {ex.WebSocketErrorCode}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Diagnostic($"{candidate.Path} endpoint {candidate.Address} rejected: {ex.Message}");
        }
        finally
        {
            ClearPeer(socket);
        }
        return false;
    }

    private async Task ReverseServerLoopAsync(CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://+:{ReversePort}/bentley/");
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 5)
        {
            // Reverse mode is optional. The main gateway path must remain quiet and usable.
            return;
        }
        catch (HttpListenerException ex)
        {
            RaiseDisconnectedStatus($"Reverse listener unavailable: {ex.Message}");
            return;
        }

        using var registration = cancellationToken.Register(() => listener.Close());
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => AcceptReversePeerAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task AcceptReversePeerAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var peer = new SocketPeer(webSocketContext.WebSocket, "Android reverse client", null, "manual fallback");
            await ReceiveLoopAsync(peer, allowIncomingAuthHello: true, cancellationToken);
            ClearPeer(peer.Socket);
            await peer.DisposeAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RaiseDisconnectedStatus($"Reverse connection error: {ex.Message}");
        }
    }

    private async Task AuthenticateOrPairPrimaryAsync(SocketPeer peer, CancellationToken cancellationToken)
    {
        var config = _configStore.Current;
        var secret = _configStore.GetSecret();
        if (config.IsPaired && secret is not null)
        {
            await SendAuthHelloAsync(peer, config.AgentId, secret, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(_pendingPairCode))
        {
            peer.PairingExchange = PairingCrypto.CreateRequest(config.AgentId, Environment.MachineName, _pendingPairCode);
            await peer.SendAsync(
                ProtocolCodec.Serialize("pair.request.v2", peer.PairingExchange.RequestPayload),
                cancellationToken);
        }
        else
        {
            RaiseConnection(false, "Phone found; enter its pairing code from the tray menu");
        }
    }

    private async Task ReceiveLoopAsync(SocketPeer peer, bool allowIncomingAuthHello, CancellationToken cancellationToken)
    {
        while (peer.IsOpen && !cancellationToken.IsCancellationRequested)
        {
            var text = await ProtocolCodec.ReceiveTextAsync(peer.Socket, cancellationToken);
            if (text is null) break;
            peer.LastSeenUtc = DateTimeOffset.UtcNow;
            ProtocolMessage message;
            try
            {
                message = ProtocolCodec.Parse(text);
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
            {
                await peer.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, ex.Message, cancellationToken);
                break;
            }

            if (await HandleControlMessageAsync(peer, message, allowIncomingAuthHello, cancellationToken))
                continue;
            if (!peer.IsAuthenticated)
            {
                await peer.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Authentication required", cancellationToken);
                break;
            }
            MessageReceived?.Invoke(message);
        }
    }

    private async Task<bool> HandleControlMessageAsync(
        SocketPeer peer,
        ProtocolMessage message,
        bool allowIncomingAuthHello,
        CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case "pair.accept.v2":
            {
                if (string.IsNullOrWhiteSpace(_pendingPairCode) || _configStore.Current.IsPaired ||
                    peer.PairingExchange is null)
                    return true;
                var serverId = message.Payload.GetProperty("serverId").GetString() ?? throw new InvalidDataException();
                if (!string.IsNullOrWhiteSpace(peer.ExpectedPhoneId) &&
                    !string.Equals(serverId, peer.ExpectedPhoneId, StringComparison.Ordinal))
                    throw new InvalidDataException("Bootstrap phone identity mismatch.");
                var secret = PairingCrypto.ValidateAndDecryptAccept(
                    peer.PairingExchange,
                    message.Payload,
                    serverId,
                    out var serverName);
                try
                {
                    _configStore.CompletePairing(serverId, serverName, secret);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(secret);
                }
                _pendingPairCode = null;
                _pairingWindow = null;
                peer.PairingExchange.Dispose();
                peer.PairingExchange = null;
                peer.IsAuthenticated = true;
                if (!TrySetActivePeer(peer, $"Connected via {peer.ConnectionPath} to {serverName}"))
                    await peer.CloseAsync(WebSocketCloseStatus.PolicyViolation, "another authenticated connection is active", cancellationToken);
                return true;
            }
            case "pair.reject":
                RaiseConnection(false, $"Pairing rejected: {GetString(message.Payload, "reason", "invalid code")}");
                return true;
            case "auth.ok":
                if (ValidateAuthOk(peer, message.Payload, out var authOkReason))
                {
                    peer.IsAuthenticated = true;
                    if (!TrySetActivePeer(peer, $"Connected via {peer.ConnectionPath} to {_configStore.Current.PhoneName ?? "phone"}"))
                        await peer.CloseAsync(WebSocketCloseStatus.PolicyViolation, "another authenticated connection is active", cancellationToken);
                }
                else
                {
                    RaiseConnection(false, $"Phone authentication failed: {authOkReason}");
                    await peer.CloseAsync(WebSocketCloseStatus.PolicyViolation, authOkReason, cancellationToken);
                }
                return true;
            case "auth.error":
                RaiseConnection(false, $"Authentication failed: {GetString(message.Payload, "reason", "unknown")}");
                return true;
            case "auth.hello" when allowIncomingAuthHello:
            {
                if (!ValidateAuthHello(message.Payload, out var reason))
                {
                    await peer.SendAsync(ProtocolCodec.Serialize("auth.error", new { reason }, message.Id), cancellationToken);
                    await peer.CloseAsync(WebSocketCloseStatus.PolicyViolation, reason, cancellationToken);
                    return true;
                }
                peer.IsAuthenticated = true;
                var config = _configStore.Current;
                var secret = _configStore.GetSecret()!;
                var timestamp = message.Payload.GetProperty("timestamp").GetInt64();
                var nonce = GetString(message.Payload, "nonce", "");
                await peer.SendAsync(ProtocolCodec.Serialize("auth.ok", new
                {
                    serverId = config.AgentId,
                    serverName = Environment.MachineName,
                    timestamp,
                    nonce,
                    proof = ComputeProof(secret, config.AgentId, timestamp, nonce)
                }, message.Id), cancellationToken);
                if (!TrySetActivePeer(peer, "Connected via manual fallback (phone initiated)"))
                    await peer.CloseAsync(WebSocketCloseStatus.PolicyViolation, "another authenticated connection is active", cancellationToken);
                return true;
            }
            case "heartbeat.ping":
                await peer.SendAsync(ProtocolCodec.Serialize("heartbeat.pong", new
                {
                    nonce = GetString(message.Payload, "nonce", "")
                }, message.Id), cancellationToken);
                return true;
            case "heartbeat.pong":
                return true;
            default:
                return false;
        }
    }

    private async Task SendAuthHelloAsync(SocketPeer peer, string clientId, byte[] secret, CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonceBytes = RandomNumberGenerator.GetBytes(18);
        var nonce = Convert.ToBase64String(nonceBytes);
        var proof = ComputeProof(secret, clientId, timestamp, nonce);
        peer.ChallengeTimestamp = timestamp;
        peer.ChallengeNonce = nonce;
        await peer.SendAsync(ProtocolCodec.Serialize("auth.hello", new
        {
            clientId,
            timestamp,
            nonce,
            proof
        }), cancellationToken);
    }

    private bool ValidateAuthHello(System.Text.Json.JsonElement payload, out string reason)
    {
        reason = "authentication failed";
        var config = _configStore.Current;
        var secret = _configStore.GetSecret();
        if (!config.IsPaired || secret is null) { reason = "not paired"; return false; }
        var clientId = GetString(payload, "clientId", "");
        if (!string.Equals(clientId, config.PhoneId, StringComparison.Ordinal)) { reason = "unknown device"; return false; }
        var timestamp = payload.TryGetProperty("timestamp", out var timestampElement) ? timestampElement.GetInt64() : 0;
        if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp) > 120_000)
        {
            reason = "clock outside authentication window";
            return false;
        }
        var nonce = GetString(payload, "nonce", "");
        if (nonce.Length < 16 || _seenNonces.ContainsKey(nonce)) { reason = "replayed nonce"; return false; }
        var suppliedProof = GetString(payload, "proof", "");
        byte[] supplied;
        byte[] expected;
        try
        {
            supplied = Convert.FromBase64String(suppliedProof);
            expected = Convert.FromBase64String(ComputeProof(secret, clientId, timestamp, nonce));
        }
        catch (FormatException)
        {
            reason = "malformed proof";
            return false;
        }
        if (!CryptographicOperations.FixedTimeEquals(supplied, expected)) { reason = "bad proof"; return false; }
        if (!_seenNonces.TryAdd(nonce, timestamp)) { reason = "replayed nonce"; return false; }
        foreach (var item in _seenNonces.Where(item => item.Value < timestamp - 300_000).ToArray())
            _seenNonces.TryRemove(item.Key, out _);
        return true;
    }

    private bool ValidateAuthOk(SocketPeer peer, System.Text.Json.JsonElement payload, out string reason)
    {
        reason = "authentication failed";
        var config = _configStore.Current;
        var secret = _configStore.GetSecret();
        if (secret is null || string.IsNullOrWhiteSpace(config.PhoneId)) { reason = "not paired"; return false; }
        var serverId = GetString(payload, "serverId", "");
        if (!string.Equals(serverId, config.PhoneId, StringComparison.Ordinal)) { reason = "unexpected phone id"; return false; }
        var timestamp = payload.TryGetProperty("timestamp", out var timestampElement) ? timestampElement.GetInt64() : 0;
        var nonce = GetString(payload, "nonce", "");
        if (timestamp != peer.ChallengeTimestamp || !string.Equals(nonce, peer.ChallengeNonce, StringComparison.Ordinal))
        {
            reason = "challenge mismatch";
            return false;
        }
        try
        {
            var supplied = Convert.FromBase64String(GetString(payload, "proof", ""));
            var expected = Convert.FromBase64String(ComputeProof(secret, serverId, timestamp, nonce));
            if (!CryptographicOperations.FixedTimeEquals(supplied, expected)) { reason = "bad server proof"; return false; }
        }
        catch (FormatException)
        {
            reason = "malformed server proof";
            return false;
        }
        peer.ChallengeNonce = null;
        return true;
    }

    private static string ComputeProof(byte[] secret, string clientId, long timestamp, string nonce)
    {
        var body = Encoding.UTF8.GetBytes($"{clientId}\n{timestamp.ToString(CultureInfo.InvariantCulture)}\n{nonce}");
        return Convert.ToBase64String(HMACSHA256.HashData(secret, body));
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await DelayAsync(10, cancellationToken);
            SocketPeer? peer;
            lock (_peerGate) peer = _activePeer;
            if (peer is not { IsOpen: true, IsAuthenticated: true }) continue;
            if (DateTimeOffset.UtcNow - peer.LastSeenUtc > TimeSpan.FromSeconds(35))
            {
                await peer.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "heartbeat timeout", cancellationToken);
                continue;
            }
            await peer.SendAsync(ProtocolCodec.Serialize("heartbeat.ping", new
            {
                nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12))
            }), cancellationToken);
        }
    }

    private bool TrySetActivePeer(SocketPeer peer, string status)
    {
        lock (_peerGate)
        {
            if (_activePeer is { IsOpen: true, IsAuthenticated: true } current && current != peer)
                return false;
            _activePeer = peer;
            peer.WasAuthenticated = true;
        }
        if (peer.RemoteAddress is not null) _configStore.SetLastKnownAddress(peer.RemoteAddress.ToString());
        RaiseConnection(true, status);
        return true;
    }

    private void ClearPeer(WebSocket socket)
    {
        var wasActive = false;
        lock (_peerGate)
        {
            if (_primaryPeer?.Socket == socket) _primaryPeer = null;
            if (_activePeer?.Socket == socket)
            {
                _activePeer = null;
                wasActive = true;
            }
        }
        if (wasActive) RaiseConnection(false, "Disconnected; reconnecting automatically");
    }

    private void RaiseConnection(bool connected, string status) => ConnectionChanged?.Invoke(connected, status);

    private void RaiseDisconnectedStatus(string status)
    {
        if (!IsConnected) RaiseConnection(false, status);
    }

    private void Diagnostic(string message)
    {
        Debug.WriteLine($"[BentleyRemote] {message}");
        RaiseDisconnectedStatus(message);
    }

    private static string GetString(System.Text.Json.JsonElement element, string property, string fallback) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static async Task DelayAsync(int seconds, CancellationToken cancellationToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private static async Task DelayWithJitterAsync(int seconds, CancellationToken cancellationToken)
    {
        var jitterMilliseconds = Random.Shared.Next(0, 501);
        try { await Task.Delay(TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(jitterMilliseconds), cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        SocketPeer? peer;
        lock (_peerGate) peer = _activePeer;
        if (peer is not null)
        {
            try { await peer.CloseAsync(WebSocketCloseStatus.NormalClosure, "Agent exiting", CancellationToken.None); }
            catch { /* best-effort shutdown */ }
        }
        var tasks = new[] { _primaryTask, _reverseTask, _heartbeatTask }.Where(task => task is not null).Cast<Task>();
        try { await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(2)); }
        catch { /* best-effort shutdown */ }
        if (_bootstrapListener is not null) await _bootstrapListener.DisposeAsync();
        _stop.Dispose();
    }

    private sealed class SocketPeer
    {
        private readonly SemaphoreSlim _sendGate = new(1, 1);

        public SocketPeer(WebSocket socket, string description, IPAddress? remoteAddress, string connectionPath,
            string? expectedPhoneId = null)
        {
            Socket = socket;
            Description = description;
            RemoteAddress = remoteAddress;
            ConnectionPath = connectionPath;
            ExpectedPhoneId = expectedPhoneId;
            LastSeenUtc = DateTimeOffset.UtcNow;
        }

        public WebSocket Socket { get; }
        public string Description { get; }
        public IPAddress? RemoteAddress { get; }
        public string ConnectionPath { get; }
        public string? ExpectedPhoneId { get; }
        public bool IsAuthenticated { get; set; }
        public bool WasAuthenticated { get; set; }
        public DateTimeOffset LastSeenUtc { get; set; }
        public long ChallengeTimestamp { get; set; }
        public string? ChallengeNonce { get; set; }
        public PairingExchange? PairingExchange { get; set; }
        public bool IsOpen => Socket.State == WebSocketState.Open;

        public async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await _sendGate.WaitAsync(cancellationToken);
            try
            {
                if (Socket.State == WebSocketState.Open)
                    await Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        public async Task CloseAsync(WebSocketCloseStatus status, string reason, CancellationToken cancellationToken)
        {
            if (Socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await Socket.CloseAsync(status, reason, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            PairingExchange?.Dispose();
            Socket.Dispose();
            _sendGate.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed record ConnectionCandidate(IPAddress Address, int Port, string Path, string? ExpectedPhoneId = null);
}
