using System.Net;
using System.Text;
using System.Threading;
using Toletus.LiteNet1.Enums;

namespace Toletus.LiteNet1;

public class LiteNetBase
{
    /*
     * Considerar mudar porta no teclado;
     * */

    internal IPEndPoint IpEndPoint;

    //protected Socket _socket;
    protected Controlador _controlador;

    /// <summary>
    /// Endereço MAC do dispositivo
    /// </summary>
    public byte[] Mac { get; }

    public bool Iniciada { get; private set; }

    protected LiteNetBase(Controlador controlador, int id, string ip, int porta, byte[] mac)
    {
        _controlador = controlador;

        Id = id;

        IpEndPoint = new IPEndPoint(IPAddress.Parse(ip), porta);

        Mac = mac;

        IntervaloHora = 99;
    }

    internal int IntervaloHora { get; set; }

    public void Iniciar()
    {
        _controlador.Carregar(_controlador.FaixasIp);

        Iniciada = Status.Equals("OK");
    }

    public void Encerrar()
    {
        _controlador.Dispose();
    }

    /// <summary>
    /// Retorna o status do dispositivo
    /// </summary>
    public string Status { get; internal set; }

    /// <summary>
    /// ID do dispositivo
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Retorna o IP do dispositivo
    /// </summary>
    public string IP => IpEndPoint.Address.ToString();
    
    /// <summary>
    /// Retorna a porta do dispositivo
    /// </summary>
    public int Porta => IpEndPoint.Port;

    /// <summary>
    /// Retorna a faixa da rede do dispositivo
    /// </summary>
    public string FaixaIP { get; internal set; }

    /// <summary>
    /// Retorna a versão do firmware
    /// </summary>
    public string VersaoFirmware { get; internal set; }

    /// <summary>
    /// Máscara da sub-rede
    /// </summary>
    public string MascaraSubRede { get; set; }

    protected Feed EnviarComando(string comando, bool aguardarRetorno = false,
        ProtocoloEnum protocolo = ProtocoloEnum.Tcp)
    {
        return EnviarComando(Encoding.ASCII.GetBytes(comando), aguardarRetorno, protocolo);
    }

    public int DuracaoAcionamento { get; set; }

    protected Feed EnviarComando(byte[] cmd, bool aguardarRetorno = false, ProtocoloEnum protocolo = ProtocoloEnum.Tcp)
    {
        var feed = new Feed
            { ID = _controlador.Feeds.Count, Cmd = cmd, LiteNet = (LiteNet)this, Protocolo = protocolo };

        _controlador.Feeds.Add(feed);

        if (!aguardarRetorno) return feed;

        var c = 0;
        while (feed.Retorno == null && c++ < 500)
        {
            Thread.Sleep(10);
        }

        return feed;
    }

    /// <summary>
    /// Modo IP do dispositivo (dinâmico ou fixo)
    /// </summary>
    public ModoIp? ModoIP { get; set; }

    /// <summary>
    /// Retorna o contador de giros no sentido horário do dispositivo
    /// </summary>
    public int ContadorHorario { get; set; }

    /// <summary>
    /// Retorna o contador de giros no sentido anti-horário do dispositivo
    /// </summary>
    public int ContadorAntiHorario { get; set; }

    public override string ToString()
    {
        var ret = $"Toletus.LiteNet nº {Id:000}, IP {IP}";

        if (Status != "OK")
            ret += "*";

        return ret;
    }
}

/// <summary>
/// Identificação ocorrrida no dispositivo
/// </summary>
public struct Identificacao
{
    public int ID;
    public DispositivoIdentificacao DispositivoIdentificacao;
    public string Valor;
    public byte[] Template;
}