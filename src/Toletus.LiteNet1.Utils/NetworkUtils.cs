using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Toletus.LiteNet1.Utils;

public class NetworkUtils
{
    private static IPAddress GetSubnetMask(IPAddress address)
    {
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        foreach (var unicastIpAddressInformation in adapter.GetIPProperties().UnicastAddresses)
            if (unicastIpAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                if (address.Equals(unicastIpAddressInformation.Address))
                    return unicastIpAddressInformation.IPv4Mask;

        return null;
    }

    public static Dictionary<string, IPAddress> ObterRedes()
    {
        var redes = new Dictionary<string, IPAddress>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        foreach (var ip in nic.GetIPProperties().UnicastAddresses)
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                if (!redes.ContainsKey(nic.Name))
                    redes.Add(nic.Name, ip.Address);

        return redes;
    }

    public static IPAddress ObterIpAddressPorNomeDaRede(string nome)
    {
        var adaptador = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == nome);

        return adaptador?.GetIPProperties().UnicastAddresses.FirstOrDefault(c => c.Address.AddressFamily == AddressFamily.InterNetwork).Address;
    }
}