// renomear sme para smi
// setar sentido entrada não está bipando
// crigar evento ongiro
// tirar bip do mensagem secundaria

using System;
using System.Runtime.InteropServices;
using System.Text;
using Toletus.LiteNet1.Enums;
using Toletus.LiteNet1.Utils;

namespace Toletus.LiteNet1;

public class LiteNet : LiteNetBase
{
    private string _mensagemSecundaria;
    private string _mensagemPadrao;

    public string MensagemPadrao
    {
        get => _mensagemPadrao;
        set => _mensagemPadrao = StringUtils.CortarPreencher(value, 16, ' ', false);
    }

    public string MensagemSecundaria
    {
        get => _mensagemSecundaria;
        set => _mensagemSecundaria = StringUtils.CortarPreencher(value, 16, ' ', false);
    }

    public bool Mudo { get; set; }
    public bool ExibirRelogio { get; set; }
    public bool ExibirContador { get; set; }
    public bool EntradaSentidoHorario { get; set; }

    public string Reservado => "00000";

    public ModoFluxo ControleFluxo { get; set; }
    public ModoEntrada ModoIdentificacao { get; set; }
    public object Tag { get; set; }

    public LiteNet(Controlador controlador, int id, string ip, int porta, byte[] mac) : base(controlador, id, ip, porta, mac)
    { }

    public Feed LiberarEntrada(string mensagem)
    {
        return EnviarComando("lgu\x00" + mensagem);            
    }

    public Feed LiberarSaida(string mensagem)
    {
        return EnviarComando("lgu\x01" + mensagem);            
    }

    public int[] GetContadores(bool aguardarRetorno = false)
    {
        var feed = EnviarComando(@"gct", aguardarRetorno);

        if (feed.Ret == null)
            return null;

        var contadores = new int[2];

        //
        byte[] bytes = { feed.Ret[0], feed.Ret[1], feed.Ret[2], feed.Ret[3] };
        var cont = BitConverter.ToInt32(bytes, 0);
        ContadorHorario = contadores[0] = cont;

        //
        bytes = new byte[]{ feed.Ret[4], feed.Ret[5], feed.Ret[6], feed.Ret[7] };
        cont = BitConverter.ToInt32(bytes, 0);
        ContadorAntiHorario = contadores[1] = cont;

        //
        return contadores;
    }

    public string SetarControleFluxo() // rever para talvez esse método incluir setar modo entrada;
    {
        var comando = Encoding.ASCII.GetBytes("stmX"); // renomear para smf

        comando[3] = (byte)ControleFluxo;

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public string SetarModoIdentificacao()
    {
        var comando = Encoding.ASCII.GetBytes("smeX"); // rever renomear para smi

        comando[3] = (byte)ModoIdentificacao;

        var feed = EnviarComando(comando);

        return feed.Retorno;
    }

    public string SetarSentidoEntrada() // rever renomear comando para; setar sentido entrada e incluí-lo no setar fluxo;
    {
        var comando = Encoding.ASCII.GetBytes("sseX");

        comando[3] = (byte)(EntradaSentidoHorario ? 1 : 0);

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public string SetarMensagemPadrao()
    {
        var comando = @"smp" + MensagemPadrao;

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public string SetarMensagemSecundaria()
    {
        return SetarMensagemSecundaria(MensagemSecundaria);
    }

    public string SetarMensagemSecundaria(string mensagem)
    {
        MensagemSecundaria = mensagem;

        var comando = @"sms" + MensagemSecundaria;

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public Feed GetID(bool aguardarRetorno = false)
    {
        var feed = EnviarComando(@"gid", aguardarRetorno);
            
        return feed;
    }

    public string GetVersao()
    {
        var feed = EnviarComando(@"gvs");
        return feed.Retorno;
    }

    public string SetID()
    {
        var comando = Encoding.ASCII.GetBytes("sidX");

        comando[3] = (byte)Id;

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public string SetMudo()
    {
        var comando = Encoding.ASCII.GetBytes("smtX");

        comando[3] = (byte)(Mudo ? 1 : 0);

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public string SetRelogio()
    {
        byte[] comando = null;

        if (ExibirRelogio)
        {
            comando = Encoding.ASCII.GetBytes("srlXHHMMSS");
            Array.Copy(StringUtils.HoraBytes, 0, comando, 4, 6);
        }
        else
            comando = Encoding.ASCII.GetBytes("srlX");

        comando[3] = (byte)(ExibirRelogio ? 1 : 0);

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public string SetContador()
    {
        var comando = Encoding.ASCII.GetBytes("sctX");

        comando[3] = (byte)(ExibirContador? 1 : 0);

        var feed = EnviarComando(comando);
        return feed.Retorno;
    }

    public string SetGeral(bool aguardarRetorno = false)
    {
        var config = new Configuracao();

        config.Comando = "mcs".ToCharArray();
        config.ControleFluxo = (byte)ControleFluxo;
        config.ModoIdentificacao = (byte)ModoIdentificacao;
        config.Mudo = (byte)(Mudo ? 1 : 0);
        config.ID = (byte)Id;
        config.DuracaoAcionamento = (byte)DuracaoAcionamento;
        config.EntradaSentidoHorario = (byte)(EntradaSentidoHorario ? 1 : 0);
        config.ExibirRelogio = (byte)(ExibirRelogio ? 1 : 0);
        config.ExibirContador = (byte)(ExibirContador ? 1 : 0);
        config.MensagemPadrao = MensagemPadrao;
        config.MensagemSecundaria = MensagemSecundaria;

        var feed = EnviarComando(config.GetBytes(), aguardarRetorno);

        return feed.Retorno;
    }

    public Feed GirouHorario()
    {
        return EnviarComando(@"ckc");
    }

    public Feed GirouAntiHorario()
    {
        return EnviarComando(@"ckc");
    }

    public struct Configuracao
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public char[] Comando;
        public byte ControleFluxo;
        public byte ModoIdentificacao;
        public byte Mudo;
        public byte ID;
        public byte ExibirRelogio;
        public byte ExibirContador;
        public byte EntradaSentidoHorario;
        public byte DuracaoAcionamento;
        public byte Reservado0;
        public byte Reservado1;
        public byte Reservado2;
        public byte Reservado3;
        public byte Reservado4;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
        public string MensagemPadrao;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
        public string MensagemSecundaria;

        public byte[] GetBytes()
        {
            var size = Marshal.SizeOf(this);
            var arr = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static Configuracao FromBytes(byte[] buffer)
        {
            var str = new Configuracao();

            try
            {
                var size = Marshal.SizeOf(str);
                var ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(buffer, 0, ptr, size);

                str = (Configuracao)Marshal.PtrToStructure(ptr, str.GetType());
                Marshal.FreeHGlobal(ptr);
            }
            catch (Exception ex)
            {
                throw new Exception("Dispositivo não compatível.");
            }

            return str;
        }
    }

    public string GetGeral()
    {
        var feed = EnviarComando("mcg", true);

        if (feed.Ret == null || feed.Ret.Length == 0)
            return null;

        if (feed.Ret.Length >= 50)
        {
            //
            var config = Configuracao.FromBytes(feed.Ret);

            ControleFluxo = (ModoFluxo)config.ControleFluxo;
            Id = config.ID;
            EntradaSentidoHorario = config.EntradaSentidoHorario == 1 ? true : false;
            ExibirRelogio = config.ExibirRelogio == 1 ? true : false;
            ExibirContador = config.ExibirContador == 1 ? true : false;
            DuracaoAcionamento = config.DuracaoAcionamento;
            ModoIdentificacao = (ModoEntrada)config.ModoIdentificacao;
            MensagemPadrao = config.MensagemPadrao;
            MensagemSecundaria = config.MensagemSecundaria;
            Mudo = (config.Mudo != 0);

            // Formata resultado em uma linha
            var resultado = string.Format(@"mcs\{0}\{1}\{2}\{3}\{4}\{5}\{6}\{7}\{8}\{9}",
                ControleFluxo,
                ModoIdentificacao,
                Mudo,
                Id,
                EntradaSentidoHorario,
                ExibirRelogio,
                ExibirContador,
                DuracaoAcionamento,
                Reservado,
                MensagemPadrao,
                MensagemSecundaria);

            return resultado;
        }
        else
            return feed.Retorno;
    }

    public Feed Resetar(bool aguardarRetorno = false)
    {
        var cmd = Encoding.ASCII.GetBytes("rst");

        var newArray = new byte[3 + Mac.Length];
        Array.Copy(cmd, newArray, cmd.Length);
        Array.Copy(Mac, 0, newArray, 3, Mac.Length);

        var feed = EnviarComando(newArray, aguardarRetorno, ProtocoloEnum.Udp);

        return feed;
    }

    public Feed ZerarContador(bool aguardarRetorno = false)
    {
        var cmd = Encoding.ASCII.GetBytes("rct");

        return EnviarComando(cmd, aguardarRetorno);
    }

    public Feed GetModoIp(bool aguardarRetorno = false)
    {
        var feed = EnviarComando(@"ipg", aguardarRetorno);

        if (aguardarRetorno && feed.Ret != null)
        {
            ModoIP = (ModoIp)feed.Ret[0];
            MascaraSubRede = string.Format("{0}.{1}.{2}.{3}", feed.Ret[1], feed.Ret[2], feed.Ret[3], feed.Ret[4]);
        }

        return feed;
    }

    public Feed SetModoIp(string ip, bool aguardarRetorno = false)
    {
        //ips#&*@ : seta as configurações de ip
        //# -> numero MAC
        //& ->Modo de IP:
        //0(ip dinâmico)
        //1(ip estático)
        //*@ -> endereço IP e máscara de subrede[Campo opcional]
        //retorna-> "ipsMok" ao configurar o modo
        //  "ipsMIok" ao configurar o modo e o ip
        //OBS: as configurações so tomam efeito ao reiniciar a placa

        var comando = Encoding.ASCII.GetBytes("ips123456112341234"); // rever renomear para smi

        //
        comando[3] = (byte)Mac[0];
        comando[4] = (byte)Mac[1];
        comando[5] = (byte)Mac[2];
        comando[6] = (byte)Mac[3];
        comando[7] = (byte)Mac[4];
        comando[8] = (byte)Mac[5];

        comando[9] = (byte)ModoIP;

        if (ModoIP == ModoIp.Fixo)
        {
            try
            {
                var camposIp = ip.Split('.');
                comando[10] = Convert.ToByte(camposIp[0]);
                comando[11] = Convert.ToByte(camposIp[1]);
                comando[12] = Convert.ToByte(camposIp[2]);
                comando[13] = Convert.ToByte(camposIp[3]);

                var camposMask = MascaraSubRede.Split('.');
                comando[14] = Convert.ToByte(camposMask[0]);
                comando[15] = Convert.ToByte(camposMask[1]);
                comando[16] = Convert.ToByte(camposMask[2]);
                comando[17] = Convert.ToByte(camposMask[3]);
            }
            catch (FormatException e)
            {
                throw;
            }

        }

        //
        var feed = EnviarComando(comando, aguardarRetorno, ProtocoloEnum.Udp);

        return feed;
    }
}