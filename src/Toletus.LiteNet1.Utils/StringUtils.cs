using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Toletus.LiteNet1.Utils;

public class StringUtils
{
    public static string ByteArrayToString(byte[] value)
    {
        return value == null ? null : EncodeNonAsciiCharacters(Encoding.UTF8.GetString(value));
    }

    public static byte[] HoraBytes
    {
        get
        {
            var hora = DateTime.Now.ToString("HHmmss");
            var ret = new byte[6];

            for (int i = 0; i < 6; i++)
                ret[i] = (byte)char.GetNumericValue(hora[i]);

            return ret;
        }
    }

    public static string EncodeNonAsciiCharacters(string value)
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

    private static string Mask(IPAddress ip, IPAddress subnetMask)
    {
        var ret = ip.GetAddressBytes();

        for (var c = 0; c < 4; c++)
            if (subnetMask.GetAddressBytes()[c] == 0)
                ret[c] = 255;

        return ret[0] + "." + ret[1] + "." + ret[2] + "." + ret[3];
    }

    public static string CortarPreencher(string texto, int tamanho, char caracter, bool alinharDireita)
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

    public static string Replicar(char caracter, int quantidade)
    {
        var retorno = string.Empty;

        for (var i = 0; i < quantidade; i++)
        {
            retorno += caracter.ToString();
        }

        return retorno;
    }
}