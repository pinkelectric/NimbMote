using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BentleyRemote.Agent.Networking;
using BentleyRemote.Agent.Security;
using BentleyRemote.Agent.SystemActions;
using BentleyRemote.Agent.Media;

var tests = new (string Name, Func<Task> Run)[]
{
    ("power allowlist and fake mapping", TestPowerActionsAsync),
    ("paired discovery signing", () => { TestPairedDiscovery(); return Task.CompletedTask; }),
    ("bootstrap proof and replay", () => { TestBootstrap(); return Task.CompletedTask; }),
    ("pairing crypto roundtrip", () => { TestPairingCrypto(); return Task.CompletedTask; }),
    ("directed broadcast", () => { TestBroadcast(); return Task.CompletedTask; }),
    ("loopback discovery responder", TestLoopbackDiscoveryAsync),
    ("artwork revision rejects stale metadata", () => { TestArtworkRevision(); return Task.CompletedTask; })
};

foreach (var test in tests)
{
    await test.Run();
    Console.WriteLine($"OK  {test.Name}");
}

static async Task TestPowerActionsAsync()
{
    foreach (var action in new[] { "lock", "sleep", "restart", "shutdown" })
    {
        var fake = new FakeController();
        var dispatcher = new SystemActionDispatcher(fake);
        var completed = new TaskCompletionSource<bool>();
        Require(dispatcher.TrySchedule(action, TimeSpan.Zero, completed.SetResult), "allowed action rejected");
        Require(await completed.Task.WaitAsync(TimeSpan.FromSeconds(2)), "fake controller failed");
        Require(fake.Calls.SequenceEqual(new[] { action }), "action mapped incorrectly");
    }
    foreach (var action in new[] { "", "format", "shutdown /s", "LOCK" })
    {
        var fake = new FakeController();
        Require(!new SystemActionDispatcher(fake).TrySchedule(action, TimeSpan.Zero), "unknown action accepted");
        Require(fake.Calls.Count == 0, "controller called for rejected action");
    }
}

static void TestPairedDiscovery()
{
    var secret = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
    var challenge = DiscoveryProtocol.CreateProbe("agent", secret, 1_800_000_000_000, "nonce-0123456789");
    var response = DiscoveryProtocol.CreateResponseForTest(challenge, "phone", "192.168.43.52", 40000, 45892, secret);
    Require(DiscoveryProtocol.TryValidateResponse(response, challenge, "phone", secret, 40000,
        new HashSet<string> { "192.168.43.52" }, 1_800_000_000_001, out var port, out _), "valid response rejected");
    Require(port == 45892, "wrong server port");
    Require(!DiscoveryProtocol.TryValidateResponse(response, challenge, "other", secret, 40000,
        new HashSet<string> { "192.168.43.52" }, 1_800_000_000_001, out _, out _), "foreign identity accepted");
    response[^2] ^= 1;
    Require(!DiscoveryProtocol.TryValidateResponse(response, challenge, "phone", secret, 40000,
        new HashSet<string> { "192.168.43.52" }, 1_800_000_000_001, out _, out _), "tampered response accepted");
}

static void TestBootstrap()
{
    const string code = "123456";
    const long timestamp = 1_800_000_000_000;
    const string nonce = "nonce-0123456789";
    var body = BootstrapDiscoveryProtocol.ProbeBody("phone-id", timestamp, nonce);
    var proof = Convert.ToBase64String(HMACSHA256.HashData(
        BootstrapDiscoveryProtocol.CodeKey(code), Encoding.UTF8.GetBytes(body)));
    var wrongProof = Convert.ToBase64String(HMACSHA256.HashData(
        BootstrapDiscoveryProtocol.CodeKey("654321"), Encoding.UTF8.GetBytes(body)));
    var data = JsonSerializer.SerializeToUtf8Bytes(new
    {
        version = 1, type = BootstrapDiscoveryProtocol.ProbeType, phoneId = "phone-id", timestamp, nonce, proof
    });
    var seen = new HashSet<string>();
    Require(BootstrapDiscoveryProtocol.TryValidateProbe(data, code, timestamp, seen, out _, out _), "valid bootstrap rejected");
    Require(!BootstrapDiscoveryProtocol.TryValidateProbe(data, code, timestamp, seen, out _, out _), "replay accepted");
    var invalidData = JsonSerializer.SerializeToUtf8Bytes(new
    {
        version = 1, type = BootstrapDiscoveryProtocol.ProbeType, phoneId = "phone-id", timestamp, nonce,
        proof = wrongProof
    });
    var invalidFirst = new HashSet<string>();
    Require(!BootstrapDiscoveryProtocol.TryValidateProbe(invalidData, code, timestamp,
        invalidFirst, out _, out _), "wrong code accepted");
    Require(BootstrapDiscoveryProtocol.TryValidateProbe(data, code, timestamp,
        invalidFirst, out _, out _), "invalid proof consumed the valid nonce");
}

static void TestPairingCrypto()
{
    const string code = "123456";
    const string clientId = "agent-id";
    const string serverId = "phone-id";
    using var exchange = PairingCrypto.CreateRequest(clientId, "PC", code);
    var request = JsonSerializer.SerializeToElement(exchange.RequestPayload);
    var timestamp = request.GetProperty("timestamp").GetInt64();
    var nonce = request.GetProperty("nonce").GetString()!;
    var clientPublicKey = request.GetProperty("publicKey").GetString()!;
    using var server = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    using var remote = ECDiffieHellman.Create();
    remote.ImportSubjectPublicKeyInfo(Convert.FromBase64String(clientPublicKey), out _);
    var serverPublicKey = Convert.ToBase64String(server.ExportSubjectPublicKeyInfo());
    var rawSecret = server.DeriveRawSecretAgreement(remote.PublicKey);
    var transcriptText = PairingCrypto.Transcript(clientId, serverId, timestamp, nonce,
        clientPublicKey, serverPublicKey);
    var transcript = Encoding.UTF8.GetBytes(transcriptText);
    var encryptionKey = PairingCrypto.Hkdf(rawSecret, PairingCrypto.BootstrapCodeKey(code), transcript, 32);
    var secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
    var iv = Enumerable.Range(20, 12).Select(value => (byte)value).ToArray();
    var encrypted = new byte[secret.Length];
    var tag = new byte[16];
    using (var aes = new AesGcm(encryptionKey, 16)) aes.Encrypt(iv, secret, encrypted, tag, transcript);
    var ciphertext = Convert.ToBase64String(encrypted.Concat(tag).ToArray());
    var ivBase64 = Convert.ToBase64String(iv);
    var responseBody = PairingCrypto.ResponseBody(clientId, serverId, timestamp, nonce,
        clientPublicKey, serverPublicKey, ivBase64, ciphertext);
    var payload = JsonSerializer.SerializeToElement(new
    {
        clientId,
        serverId,
        serverName = "Galaxy",
        timestamp,
        nonce,
        publicKey = serverPublicKey,
        iv = ivBase64,
        ciphertext,
        proof = PairingCrypto.SignCode(code, responseBody)
    });
    var decrypted = PairingCrypto.ValidateAndDecryptAccept(exchange, payload, serverId, out var serverName);
    Require(serverName == "Galaxy" && decrypted.SequenceEqual(secret), "ECDH/AES-GCM secret mismatch");
    CryptographicOperations.ZeroMemory(rawSecret);
    CryptographicOperations.ZeroMemory(encryptionKey);
    CryptographicOperations.ZeroMemory(decrypted);
}

static async Task TestLoopbackDiscoveryAsync()
{
    var secret = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
    using var responder = new System.Net.Sockets.UdpClient(
        new IPEndPoint(IPAddress.Loopback, LanDiscovery.DiscoveryPort));
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var responderTask = Task.Run(async () =>
    {
        var received = await responder.ReceiveAsync(timeout.Token);
        using var json = JsonDocument.Parse(received.Buffer);
        var root = json.RootElement;
        var challenge = new DiscoveryChallenge(
            root.GetProperty("clientId").GetString()!,
            root.GetProperty("timestamp").GetInt64(),
            root.GetProperty("nonce").GetString()!,
            received.Buffer);
        var response = DiscoveryProtocol.CreateResponseForTest(challenge, "phone-id",
            IPAddress.Loopback.ToString(), received.RemoteEndPoint.Port, 45892, secret);
        await responder.SendAsync(response, received.RemoteEndPoint, timeout.Token);
    }, timeout.Token);
    var results = await LanDiscovery.DiscoverAsync("agent-id", "phone-id", secret, null, timeout.Token,
        new[] { new IPEndPoint(IPAddress.Loopback, LanDiscovery.DiscoveryPort) });
    await responderTask;
    Require(results.Count == 1 && results[0].Address.Equals(IPAddress.Loopback) && results[0].Port == 45892,
        "loopback signed responder was not selected");
}

static void TestBroadcast() => Require(
    LanDiscovery.GetDirectedBroadcast(IPAddress.Parse("192.168.43.52"), IPAddress.Parse("255.255.255.0"))
        .Equals(IPAddress.Parse("192.168.43.255")), "broadcast calculation failed");

static void TestArtworkRevision()
{
    var tracker = new ArtworkRevisionTracker();
    Require(tracker.Begin("edge\nold", out var oldRevision), "first metadata was not marked new");
    Require(tracker.Begin("edge\nnew", out var newRevision), "changed Edge metadata was not marked new");
    Require(!tracker.IsCurrent(oldRevision), "late old thumbnail would be accepted");
    Require(tracker.IsCurrent(newRevision), "current thumbnail was rejected");
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FakeController : ISystemPowerController
{
    public List<string> Calls { get; } = [];
    public bool Lock() { Calls.Add("lock"); return true; }
    public bool Sleep() { Calls.Add("sleep"); return true; }
    public bool Restart() { Calls.Add("restart"); return true; }
    public bool Shutdown() { Calls.Add("shutdown"); return true; }
}
