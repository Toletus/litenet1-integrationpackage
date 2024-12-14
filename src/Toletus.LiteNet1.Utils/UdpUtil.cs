using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Toletus.LiteNet1.Utils;

public class UdpUtil
{
    private Socket _sock;
    private byte[] _buffer;

    // delegate declaration 
    public delegate void RetornoHandler(EndPoint endPoint, byte[] retorno);

    // event declaration 
    public event RetornoHandler OnRetorno;

    public UdpUtil()
    {
        using var udpClient = new UdpClient(7879);
        udpClient.Close();
    }

    public int Enviar(string comando, IPAddress faixaIP, int porta)
    {
        return Enviar(Encoding.ASCII.GetBytes(comando), faixaIP, porta);
    }

    public int Enviar(byte[] cmd, IPAddress faixaIP, int porta)
    {
        //https://acrocontext.wordpress.com/2013/08/15/c-simple-udp-listener-in-asynchronous-way/
        EndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, porta);
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        _sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
        _sock.Bind(new IPEndPoint(faixaIP, 0));
        var ret = _sock.SendTo(cmd, endPoint);

        //Setup the socket and message buffer
        _buffer = new byte[1024];

        //Start listening for a new message.
        _sock.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref endPoint, DoReceiveFrom, _sock);
            
        return ret;
    }

    private void DoReceiveFrom(IAsyncResult iar)
    {
        try
        {
            //Get the received message.
            var recvSock = (Socket)iar.AsyncState;
            EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
            var msgLen = recvSock.EndReceiveFrom(iar, ref clientEP);
            var localMsg = new byte[msgLen];

            Array.Copy(_buffer, localMsg, msgLen);

            OnRetorno?.Invoke(clientEP, localMsg);

            //Start listening for a new message.
            EndPoint newClientEp = new IPEndPoint(IPAddress.Any, 0);
            _sock.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref newClientEp, DoReceiveFrom, _sock);
        }
        catch (ObjectDisposedException)
        {
            throw;
            //expected termination exception on a closed socket.
            // ...I'm open to suggestions on a better way of doing this.
        }
    }

    public byte[] EnviarEAguardar(byte[] cmd, IPAddress faixaIP, int porta)
    {
        _ret = null;
        OnRetorno += UdpUtil_OnRetorno;
        Enviar(cmd, faixaIP, porta);

        var c = 0;

        while (_ret == null && c++ < 500)
        {
            Thread.Sleep(10);
        }

        if (_ret == null)
            _ret = Encoding.ASCII.GetBytes("Timeout");

        OnRetorno = null;

        return _ret;
    }

    private byte[] _ret;
    private void UdpUtil_OnRetorno(EndPoint endPoint, byte[] retorno)
    {
        _ret = retorno;
    }
}