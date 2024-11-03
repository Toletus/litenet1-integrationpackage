using System;

namespace Toletus.LiteNet1;

public class Feed
{
    public int ID { get; internal set; }
    public DateTime DataHora { get; internal set; }
    public double? TS { get; internal set; }

    private string _comando;
    public string Comando
    {
        get
        {
            if (_comando == null && _cmd != null)
                _comando = Util.ByteArrayToString(_cmd);

            return _comando;
        }
        internal set => _comando = value;
    }

    private byte[] _cmd;
    public byte[] Cmd
    {
        get
        {
            if (_cmd == null)
                _cmd = System.Text.Encoding.UTF8.GetBytes(Comando);

            return _cmd;
        }
        internal set => _cmd = value;
    }

    public string Retorno { get; internal set; }

    public byte[] Ret { get; internal set; }

    public LiteNet LiteNet { get; internal set; }

    public Identificacao Identificacao { get; internal set; }

    public object Tag { get; internal set; }

    public ProtocoloEnum Protocolo { get; internal set; }
}

public enum ProtocoloEnum
{ Tcp, Udp }