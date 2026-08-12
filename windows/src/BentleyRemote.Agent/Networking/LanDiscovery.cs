using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BentleyRemote.Agent.Networking;

internal static class LanDiscovery
{
    public const int DiscoveryPort = 45894;
    private static readonly TimeSpan ResponseWindow = TimeSpan.FromSeconds(2);

    public static async Task<IReadOnlyList<DiscoveredEndpoint>> DiscoverAsync(
        string clientId,
        string expectedServerId,
        byte[] secret,
        Action<string>? diagnostic,
        CancellationToken cancellationToken,
        IReadOnlyList<IPEndPoint>? overrideTargets = null)
    {
        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.EnableBroadcast = true;
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        var localPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
        var localAddresses = GetLocalIPv4Addresses();
        var challenge = DiscoveryProtocol.CreateProbe(clientId, secret);
        var targets = overrideTargets ?? GetBroadcastTargets();
        if (targets.Count == 0)
        {
            diagnostic?.Invoke("LAN discovery skipped: no active IPv4 LAN interface");
            return Array.Empty<DiscoveredEndpoint>();
        }

        foreach (var target in targets.DistinctBy(endpoint => endpoint.ToString()))
        {
            try
            {
                await udp.SendAsync(challenge.Datagram, target, cancellationToken);
            }
            catch (SocketException ex)
            {
                diagnostic?.Invoke($"LAN discovery broadcast {target.Address} rejected: {ex.SocketErrorCode}");
            }
        }

        var results = new Dictionary<string, DiscoveredEndpoint>(StringComparer.Ordinal);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ResponseWindow);
        while (!timeout.IsCancellationRequested)
        {
            UdpReceiveResult received;
            try { received = await udp.ReceiveAsync(timeout.Token); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { break; }
            catch (SocketException ex)
            {
                diagnostic?.Invoke($"LAN discovery receive failed: {ex.SocketErrorCode}");
                break;
            }

            if (received.RemoteEndPoint.AddressFamily != AddressFamily.InterNetwork) continue;
            if (!DiscoveryProtocol.TryValidateResponse(
                    received.Buffer,
                    challenge,
                    expectedServerId,
                    secret,
                    localPort,
                    localAddresses,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    out var serverPort,
                    out var reason))
            {
                diagnostic?.Invoke($"LAN discovery response from {received.RemoteEndPoint.Address} rejected: {reason}");
                continue;
            }

            // The address is intentionally taken from the authenticated UDP source endpoint.
            var endpoint = new DiscoveredEndpoint(received.RemoteEndPoint.Address, serverPort);
            results[endpoint.Address.ToString()] = endpoint;
            diagnostic?.Invoke($"LAN discovery authenticated {endpoint.Address}:{endpoint.Port}");
        }
        return results.Values.ToArray();
    }

    internal static IReadOnlyList<IPEndPoint> GetBroadcastTargets()
    {
        var addresses = new HashSet<IPAddress>();
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up ||
                adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;
            try
            {
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork ||
                        IPAddress.IsLoopback(unicast.Address) || IsLinkLocal(unicast.Address))
                        continue;
                    var mask = unicast.IPv4Mask;
                    if (mask is null) continue;
                    addresses.Add(GetDirectedBroadcast(unicast.Address, mask));
                }
            }
            catch (NetworkInformationException) { }
        }
        addresses.Add(IPAddress.Broadcast);
        return addresses.Select(address => new IPEndPoint(address, DiscoveryPort)).ToArray();
    }

    internal static IPAddress GetDirectedBroadcast(IPAddress address, IPAddress mask)
    {
        var addressBytes = address.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();
        if (addressBytes.Length != 4 || maskBytes.Length != 4)
            throw new ArgumentException("IPv4 address and mask are required.");
        return new IPAddress(addressBytes.Zip(maskBytes, (value, maskPart) =>
            (byte)(value | (byte)~maskPart)).ToArray());
    }

    private static HashSet<string> GetLocalIPv4Addresses()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up) continue;
            try
            {
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                        result.Add(unicast.Address.ToString());
                }
            }
            catch (NetworkInformationException) { }
        }
        result.Add(IPAddress.Loopback.ToString());
        return result;
    }

    private static bool IsLinkLocal(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254;
    }
}

internal sealed record DiscoveredEndpoint(IPAddress Address, int Port);
