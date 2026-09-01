using System.Net;
using System.Net.NetworkInformation;

namespace BentleyRemote.Agent.Networking;

internal static class GatewayDiscovery
{
    public static IReadOnlyList<IPAddress> FindIPv4Gateways()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
            .Where(adapter => adapter.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel)
            .SelectMany(adapter =>
            {
                try
                {
                    var rank = adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 : 1;
                    return adapter.GetIPProperties().GatewayAddresses
                        .Select(address => address.Address)
                        .Where(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Where(address => !IPAddress.Any.Equals(address) && !IPAddress.None.Equals(address))
                        .Select(address => (address, rank));
                }
                catch (NetworkInformationException)
                {
                    return Array.Empty<(IPAddress address, int rank)>();
                }
            })
            .OrderBy(item => item.rank)
            .Select(item => item.address)
            .Distinct()
            .ToArray();
    }
}

