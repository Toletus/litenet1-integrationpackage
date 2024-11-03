using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Toletus.LiteNet1;

public class SocketUtil
{
    internal static int TimeOut = 5000;

    internal static void Send(Socket socket, byte[] buffer, int offset, int size, int timeout)
    {
        var startTickCount = Environment.TickCount;
        var sent = 0;  // how many bytes is already sent

        do
        {
            if (Environment.TickCount > startTickCount + timeout)
                throw new Exception("Timeout.");

            try
            {   
                sent += socket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock ||
                    ex.SocketErrorCode == SocketError.IOPending ||
                    ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                {
                    // socket buffer is probably full, wait and try again
                    Thread.Sleep(30);
                }
                else
                    throw ex;  // any serious error occurr
            }
        } while (sent < size);
    }

    internal static byte[] Receive(Socket socket, int timeout)
    {
        socket.ReceiveTimeout = 1000;
            
        var buffer = new byte[512];

        do
        {
            int received;

            try
            {
                received = socket.Receive(buffer, 0, buffer.Length, SocketFlags.Partial);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock ||
                    ex.SocketErrorCode == SocketError.IOPending ||
                    ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                {
                    // socket buffer is probably empty, wait and try again
                    Thread.Sleep(10);
                    continue;
                }
                else
                    throw ex;  // any serious error occurr
            }

            // Resize the array
            Array.Resize(ref buffer, received);

            break;
        } while (true);


        return buffer;
    }

    internal static byte[] SendCommand(byte[] comando, IPEndPoint endPoint)
    {
        try
        {
            var tentativaConectar = 0;
            while (tentativaConectar++ < 3)
            {
                var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = 10000
                };

                // Connect using a timeout
                var result = socket.BeginConnect(endPoint, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeOut, true);

                if (!success)
                {
                    // NOTE, MUST CLOSE THE SOCKET

                    socket.Close();
                    return Encoding.UTF8.GetBytes(string.Format("ConnectionFailed - {0}:{1}", ((IPEndPoint)endPoint).Address, endPoint.Port));
                }

                if (socket.Connected)
                {
                    try
                    {
                        // sends the text with timeout                                         
                        Send(socket, comando, 0, comando.Length, TimeOut);

                        byte[] buff = null;

                        buff = Receive(socket, TimeOut);

                        if (buff == null)
                            return null;
                        else if (buff.Length == 0)
                            return null;
                        else if (buff != null)
                        {
                            if (tentativaConectar > 1)
                            { }

                            return buff;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Não é possível bloquear uma chamada neste soquete enquanto uma chamada assíncrona anterior estiver em andamento. na linha: bool success = result.AsyncWaitHandle.WaitOne(10000, true);em 
                        //throw;
                    }
                }
            }

            return Encoding.UTF8.GetBytes("Nao conectou");
        }
        catch (SocketException se)
        {
            return Encoding.UTF8.GetBytes("ConnectionFailed ex");
        }
    }
}