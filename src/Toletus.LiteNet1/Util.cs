using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Toletus.LiteNet1;

public class Util
{
    internal static string ByteArrayToString(byte[] value)
    {
        return value == null ? null : EncodeNonAsciiCharacters(Encoding.UTF8.GetString(value));
    }

    internal static byte[] Hora
    {
        get
        {
            var hora = DateTime.Now.ToString("HHmmss");
            var ret = new byte[6];
            ret[0] = (byte)int.Parse(hora[0].ToString());
            ret[1] = (byte)int.Parse(hora[1].ToString());
            ret[2] = (byte)int.Parse(hora[2].ToString());
            ret[3] = (byte)int.Parse(hora[3].ToString());
            ret[4] = (byte)int.Parse(hora[4].ToString());
            ret[5] = (byte)int.Parse(hora[5].ToString());

            return ret;
        }
    }

    internal static string EncodeNonAsciiCharacters(string value)
    {
        var ret = string.Empty;

        foreach (var c in value)
        {
            if (c > 127 || c < 32)
                ret += "\\" + ((int)c);
            else
                ret += c;
        }

        return ret;
    }

    /// <summary>
    /// Retorna lista com as faixas de Ip locais, use uma das faixas para carregar seus dispositivos.
    /// </summary>
    public static List<string> FaixasIPLocal
    {
        get
        {
            var host = Dns.GetHostEntry(string.Empty);

            return (from ip in host.AddressList where ip.AddressFamily == AddressFamily.InterNetwork select ip.ToString()).ToList();
        }
    }

    private static IPAddress GetSubnetMask(IPAddress address)
    {
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        foreach (var unicastIpAddressInformation in adapter.GetIPProperties().UnicastAddresses)
            if (unicastIpAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                if (address.Equals(unicastIpAddressInformation.Address))
                    return unicastIpAddressInformation.IPv4Mask;

        return null;
    }

    private static string Mask(IPAddress ip, IPAddress subnetMask)
    {
        var ret = ip.GetAddressBytes();

        for (var c = 0; c < 4; c++)
            if (subnetMask.GetAddressBytes()[c] == 0)
                ret[c] = 255;

        return ret[0] + "." + ret[1] + "." + ret[2] + "." + ret[3];
    }

    internal static string CortarPreencher(string texto, int tamanho, char caracter, bool alinharDireita)
    {
        if (texto == null)
            texto = string.Empty;

        if (texto.Length > tamanho)
            texto = texto.Substring(0, tamanho);
        else
        {
            if (alinharDireita)
                texto = Replicar(caracter, tamanho - texto.Length) + texto;
            else
                texto = texto + Replicar(caracter, tamanho - texto.Length);
        }

        return texto;
    }

    internal static string Replicar(char caracter, int quantidade)
    {
        var retorno = string.Empty;

        for (var i = 0; i < quantidade; i++)
        {
            retorno += caracter.ToString();
        }

        return retorno;
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