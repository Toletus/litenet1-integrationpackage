using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Toletus.LiteNet1.Enums;
using Toletus.LiteNet1.Utils;

namespace Toletus.LiteNet1;

/// <summary>
/// Através do controlador poderemos gerenciar todos os dispositivos Toletus em sua rede.
/// </summary>
public class Controlador : ControladorBase
{
    public List<string> FaixasIp { get; set; }
    public delegate void LitenetHandler(LiteNet device);
    public LitenetHandler OnLiteNet;
    private AutoResetEvent _waitHandle;

    public void Carregar(string faixaIP, AutoResetEvent waitHandle = null)
    {
        _waitHandle = waitHandle;
        var faixasIp = new List<string> { faixaIP };
        Carregar(faixasIp);

        if (_waitHandle != null)
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2000);
                _waitHandle.Set();
            });
    }

    public void Carregar(List<string> faixasIp)
    {
        FaixasIp = faixasIp;
        LiteNets = new List<LiteNet>();
        Iniciar();

        foreach (var faixa in faixasIp)
        {
            var ip = NetworkUtils.ObterIpAddressPorNomeDaRede(faixa) ?? IPAddress.Parse(faixa);

            AcharDispositivos(ip);
            FaixaIp = ip;
        }
    }

    public bool AcharDispositivos(IPAddress faixaIP)
    {
        LiteNets ??= [];
        var udp = new UdpUtils();
        udp.OnRetorno += Udp_OnRetorno;
        udp.Enviar("prc", faixaIP, 1001);

        return (LiteNets.Count > 0);
    }

    private void Udp_OnRetorno(EndPoint endPoint, byte[] retorno)
    {
        var ret = StringUtils.ByteArrayToString(retorno);

        if (!ret.StartsWith("Toletus")) return;

        var versaoFW = $"{retorno[14]}.{retorno[15]}";

        //Handle the received message
        var mac = new byte[6];
        mac[0] = retorno[retorno.Length - 7];
        mac[1] = retorno[retorno.Length - 6];
        mac[2] = retorno[retorno.Length - 5];
        mac[3] = retorno[retorno.Length - 4];
        mac[4] = retorno[retorno.Length - 3];
        mac[5] = retorno[retorno.Length - 2];

        int id = retorno[retorno.Length - 1];

        //Do other, more interesting, things with the received message.
        var liteNet = new LiteNet(this, id, ((IPEndPoint)endPoint).Address.ToString(), ((IPEndPoint)endPoint).Port, mac);
        LiteNets.Add(liteNet);
        liteNet.VersaoFirmware = versaoFW;
        liteNet.ModoIP = ModoIp.Dinamico;
        VerificarStatus();

        OnLiteNet?.Invoke(liteNet);

        _waitHandle?.Set();
    }

    private void Iniciar()
    {
        Maquina();
    }

    private void VerificarStatus()
    {
        for (var a = 0; a < LiteNets.Count; a++)
        {
            LiteNets[a].Status = "OK";

            for (var b = 0; b < LiteNets.Count; b++)
            {
                if (b == a) continue;

                if (LiteNets[b].IP == LiteNets[a].IP)
                    LiteNets[a].Status = "IP Conflitante";
            }
        }
    }

    public void EncerrarTodosOsDispositivos()
    {
        foreach (var item in LiteNets)
            item.Encerrar();

        _bw?.CancelAsync();

        LiteNets = new List<LiteNet>();
    }

    /// <summary>
    /// Suspende o controlador, não ocorrerá comunicação com nenhum dispositivo controlado.
    /// </summary>
    public void Suspender()
    {
        Suspenso = true;
    }

    /// <summary>
    /// Resume o controlador previmente suspenso, voltará a ocorrer a comunicação com os dispositivo controlados.
    /// </summary>
    public void Resumir()
    {
        Suspenso = false;
    }

    public int? GetLiteNetIndexOnControllerById(int id)
    {
        int? i = null;

        for (var c = 0; c < LiteNets.Count; c++)
            if (LiteNets[c].Id == id)
            {
                i = c;
                break;
            }

        return i;
    }

    public LiteNet GetLiteNetById(int id)
    {
        return LiteNets.FirstOrDefault(t => t.Id == id);
    }
}