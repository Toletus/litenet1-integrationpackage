using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using Toletus.LiteNet1.Enums;
using Toletus.LiteNet1.Utils;

namespace Toletus.LiteNet1;

public class ControladorBase : IDisposable
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public BindingList<Feed> Feeds = new BindingList<Feed>();

    protected BackgroundWorker _bw;
    protected bool Suspenso { get; set; }

    private int _ultimoFeedProcessado = -1;
    private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);
    private readonly byte[] QryCmd = new byte[3] { 113, 114, 121 };

    protected void Maquina()
    {
        if (_bw != null && _bw.IsBusy) return;

        Suspenso = false;
        _bw = new BackgroundWorker();
        _bw.WorkerReportsProgress = true;
        _bw.WorkerSupportsCancellation = true;
        _bw.DoWork += _bw_DoWork;
        _bw.ProgressChanged += _bw_ProgressChanged;
        _bw.RunWorkerAsync();
    }

    #region Propriedades
    private int _ultimoLiteNetQry = -1;

    private LiteNet ProximoLiteNetQry
    {
        get
        {
            try
            {
                if (_ultimoLiteNetQry >= (LiteNets.Count - 1))
                    _ultimoLiteNetQry = -1;

                _ultimoLiteNetQry++;

                return LiteNets[_ultimoLiteNetQry];
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    private List<LiteNet> _liteNets;

    /// <summary>
    /// Lista de dispositivos controlados
    /// </summary>
    public List<LiteNet> LiteNets
    {
        get => _liteNets;
        protected set => _liteNets = value;
    }
    #endregion

    //private int _conthora = 99;

    /// <summary>
    /// Considerações para o futuro:
    /// Criar thread para processar cada dispositivo separadamente, como está, se um parar vai atrasar os demais. Penso em talves deixar essa Maquina (poll), dentro do dispositivo.
    /// Ou ao invez de mais thread, receber o retorno de foram assíncrona.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _bw_DoWork(object sender, DoWorkEventArgs e)
    {
        try
        {
            byte[] ret;
            string s;

            while (true)
            {
                if (Suspenso)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // check status on each step
                if (_bw.CancellationPending)
                {
                    e.Cancel = true;
                    break; // abort work, if it's cancelled
                }

                //
                ret = null;
                Feed feed = null;

                if (Feeds.Count - 1 > _ultimoFeedProcessado)//  this._comando != null)
                    feed = Feeds[++_ultimoFeedProcessado];
                else if (LiteNets != null && LiteNets.Count > 0) // sem feeds na fila
                {
                    feed = new Feed();
                    feed.LiteNet = ProximoLiteNetQry;
                    feed.Cmd = QryCmd;

                    if (feed.LiteNet.ExibirRelogio)
                    {
                        feed.LiteNet.IntervaloHora++;
                        if (feed.LiteNet.IntervaloHora == 100)
                        {
                            var cmd = new byte[9];
                            Array.Copy(QryCmd, cmd, 3);
                            Array.Copy(StringUtils.HoraBytes, 0, cmd, 3, 6);
                            feed.Cmd = cmd;
                            feed.LiteNet.IntervaloHora = 0;
                        }
                    }
                }

                if (feed != null)
                {
                    feed.DataHora = DateTime.Now;

                    if (feed.Protocolo == ProtocoloEnum.Tcp)
                    {
                        if (feed.Comando != "qry") Logger.Debug($"LiteNet1 {nameof(feed.Comando)} {feed.Comando} {nameof(feed.LiteNet.IpEndPoint)} {feed.LiteNet.IpEndPoint} {nameof(feed.LiteNet.Id)} {feed.LiteNet.Id}  {nameof(feed.LiteNet.Tag)} {feed.LiteNet.Tag}");
                        ret = SocketUtils.SendCommand(feed.Cmd, feed.LiteNet.IpEndPoint);
                    }
                    else
                    {
                        var udp = new UdpUtils();
                        ret = udp.EnviarEAguardar(feed.Cmd, FaixaIp, 1001);
                    }

                    if (ret != null && ret.Length > 0)
                    {
                        s = StringUtils.ByteArrayToString(ret); // avaliar mudar isso aqui, não precisar de converter em string toda vez.

                        if (feed != null) // não é qry
                        {
                            feed.TS = DateTime.Now.Subtract(feed.DataHora).TotalSeconds;

                            feed.Retorno = s;
                            feed.Ret = ret;
                        }

                        //if (feed.LiteNet.IntervaloHora == 0)
                        //{
                        //    s = "qrywat\\0";
                        //}

                        //descomente para aparecer o feed de query sem retorno na lista de feeds.
                        //if (s == "\\0")
                        //    s = "qrywat\\0";

                        if (s.StartsWith("qry"))
                        {
                            var identificacao = new Identificacao();

                            switch (s.Substring(3, 3))          /// CONSIDERAR mudar retorno para um byte compatível com a Enum para fazer um cast
                            {
                                case "FGR":
                                    identificacao.DispositivoIdentificacao = DispositivoIdentificacao.ImpressaoDigitalTemplate;
                                    identificacao.Template = new byte[ret.Length - 7];
                                    Array.Copy(ret, 7, identificacao.Template, 0, ret.Length - 7);
                                    break;
                                case "KPD":
                                    identificacao.DispositivoIdentificacao = DispositivoIdentificacao.Teclado;
                                    identificacao.Valor = s.Substring(7);
                                    break;
                                case "RID":
                                    identificacao.DispositivoIdentificacao = DispositivoIdentificacao.CartaoProximidade;
                                    for (var i = 7; i < ret.Length; i++)
                                        identificacao.Valor += "S:" + ret[i].ToString("X");
                                    break;
                                case "BAR": // CONFIRMAR// CONFIRMAR// CONFIRMAR// CONFIRMAR// CONFIRMAR// CONFIRMAR// CONFIRMAR
                                    identificacao.DispositivoIdentificacao = DispositivoIdentificacao.CartaoBarras;
                                    for (var i = 7; i < ret.Length; i++)
                                        identificacao.Valor += "B:" + ret[i].ToString("X");
                                    break;
                                default:
                                    identificacao.DispositivoIdentificacao = DispositivoIdentificacao.Indefinido;
                                    identificacao.Valor = s.Substring(7);
                                    break;
                            }

                            feed.Identificacao = identificacao;

                            _bw.ReportProgress(0, feed);
                        }
                        else if (s.StartsWith("ckc"))
                        {
                            _bw.ReportProgress(1, feed);
                        }
                        else if (s.StartsWith("tmo")) // timeout
                        {
                            _bw.ReportProgress(2, feed);
                        }
                    }
                }

                var div = LiteNets.Count;
                if (div == 0)
                    div = 1;

                Thread.Sleep(50 / div);

                // adicional sleep, pq está em modo qry, sem feeds na fila para processar
                if (Feeds.Count - 1 <= _ultimoFeedProcessado)
                    Thread.Sleep(50 / div);
            }

            _resetEvent.Set(); // signal that worker is done
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private void _bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        var feed = (Feed)e.UserState;

        feed.ID = Feeds.Count;

        _ultimoFeedProcessado++;
        Feeds.Add(feed);

        if (e.ProgressPercentage == 0)
        {
            if (OnIdentificacao != null)
                OnIdentificacao(feed.LiteNet, feed.Identificacao);
        }
        else if (e.ProgressPercentage == 1)
        {

            //
            byte[] bytes = { feed.Ret[5], feed.Ret[6], feed.Ret[7], feed.Ret[8] };
            var cont = BitConverter.ToInt32(bytes, 0);

            //
            if (feed.Ret[4] == 1)
                feed.LiteNet.ContadorAntiHorario = cont;
            else
                feed.LiteNet.ContadorHorario = cont;

            if (OnAcessou != null)
                OnAcessou(feed.LiteNet, true, (Sentido)(int)(byte)feed.Ret[3]);
        }
        else if (e.ProgressPercentage == 2)
        {
            feed.Retorno = "Timeout ";

            if ((byte)feed.Ret[3] == 0)
                feed.Retorno += "Entrada";
            else if ((byte)feed.Ret[3] == 1)
                feed.Retorno += "Saida";

            if (OnAcessou != null)
                OnAcessou(feed.LiteNet, false, (Sentido)(int)(byte)feed.Ret[3]);
            else if (OnRetornoComando != null)
                OnRetornoComando(feed.LiteNet, e.UserState.ToString());
        }
    }

    /// <summary>
    /// Retorna faixa de IP do dispositivo.
    /// </summary>
    public IPAddress FaixaIp
    {
        get; internal set;
    }

    #region Eventos
    // delegate declaration 
    public delegate void IdentificacaoHandler(LiteNet sender, Identificacao identificacao);
    public delegate void AcessouHandler(LiteNet sender, bool sucesso, Sentido sentido);
    public delegate void RetornoComandoHandler(LiteNet sender, string retorno);

    // event declaration 
    /// <summary>
    /// Evento disparado quando há uma idendicação no teclado, ou leitor embarcado no dispositivo.
    /// </summary>
    public event IdentificacaoHandler OnIdentificacao;

    /// <summary>
    /// Evento disparado quando ocorrer um acesso (giro) no dispositivo
    /// </summary>
    public event AcessouHandler OnAcessou;

    /// <summary>
    /// Evento disparado em resposta a algum comando enviado
    /// </summary>
    public event RetornoComandoHandler OnRetornoComando;
    #endregion

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
                ((IDisposable)_bw).Dispose();
                _resetEvent.Dispose(); // não sei se o ideal é aqui // rever
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~ControladorBase() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }
    #endregion
}