using MessagePack;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public class ServiceConnection : IDisposable
    {
        readonly IPEndPoint localEndPoint;
        TcpClient client;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        MessagePackStreamReader streamReader;

        public bool Connected
        {
            get
            {
                return !(client == null || !client.Connected 
                    || client.Client.Poll(50, SelectMode.SelectRead) || client.Available == 0);
            }
        }

        public ServiceConnection(ServiceConstant sc)
#if DEBUG
            : this(IPAddress.Loopback, sc.Port)
#else
            : this(Dns.GetHostAddresses(sc.ServiceName).FirstOrDefault(), sc.Port)
#endif
        {
        }

        public ServiceConnection(IPAddress addr, int port)
        {
            localEndPoint = new IPEndPoint(addr, port);
        }

        public void Dispose()
        {
             _tokenSource.Cancel();
            try
            {
                if (client.Connected)
                {
                    client.Client.Shutdown(SocketShutdown.Both);
                    streamReader.Dispose();
                    client.GetStream().Close();
                    client.Dispose();
                }
            }
            catch (Exception) { }
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                client = new TcpClient(AddressFamily.InterNetwork);
#if DEBUG
                await client.ConnectAsync(IPAddress.Loopback, localEndPoint.Port).ConfigureAwait(false);
#else
                await client.ConnectAsync(localEndPoint.Address, localEndPoint.Port).ConfigureAwait(false);
#endif
                streamReader = new MessagePackStreamReader(client.GetStream());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }

        public async Task<string> SendAsync(UserCommandType userCommand, bool receive = false)
        {
            if (_tokenSource.IsCancellationRequested) return null;
            string result = "";
            ServiceResult sr;
            try
            {
                await MessagePackSerializer.SerializeAsync<UserCommandType>(client.GetStream(), userCommand).ConfigureAwait(false);
                if (receive)
                {
                    ReadOnlySequence<byte>? msgpack;
                    msgpack = await streamReader.ReadAsync(_tokenSource.Token).ConfigureAwait(false);
                    if (!msgpack.HasValue)
                    {
                        if (!_tokenSource.IsCancellationRequested)
                        {
                            Dispose();
                        }
                        return null;
                    }
                    sr = MessagePackSerializer.Deserialize<ServiceResult>(msgpack.Value, cancellationToken: _tokenSource.Token);
                    result = sr.Message;
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return result;
        }

        public async Task<string> Send(string data, bool receive = false)
        {
            string result = "";
            try
            {
                TcpClient tcpClient = new TcpClient(AddressFamily.InterNetwork);
                await tcpClient.ConnectAsync(localEndPoint.Address, localEndPoint.Port).ConfigureAwait(false);
                StreamReader client_in = null;
                StreamWriter client_out = null;
                try
                {
                    client_in = new StreamReader(tcpClient.GetStream());
                    client_out = new StreamWriter(tcpClient.GetStream());

                    await client_out.WriteAsync(data).ConfigureAwait(false);
                    await client_out.FlushAsync().ConfigureAwait(false);
                    tcpClient.Client.Shutdown(SocketShutdown.Send);

                    if (receive)
                    {
                        result = await client_in.ReadToEndAsync().ConfigureAwait(false);
                    }
                    tcpClient.Client.Shutdown(SocketShutdown.Receive);
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
                tcpClient.Client.Close();
                tcpClient.Dispose();
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
            return result;
        }
    }

    [MessagePackObject]
    public class ServiceResult
    {
        [Key(0)]
        public string Message { get; set; }
    }
}
