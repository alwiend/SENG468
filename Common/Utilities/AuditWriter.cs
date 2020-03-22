using MessagePack;
using System;
using System.Collections.Concurrent;
using Utilities;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public class AuditWriter : IAuditWriter
    {
        readonly ConcurrentStack<object> logs = new ConcurrentStack<object>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private TcpClient client = null;
        public bool Connected
        {
            get
            {
                return client != null && client.Connected;
            }
        }

        public AuditWriter()
        {
            Task.Run(KeepConnected);
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            client.Client.Shutdown(SocketShutdown.Both);
            client.GetStream().Close();
            client.Close();
            client.Dispose();
        }

        /*
         * @param record The event to pass to the Audit server
         */
        public void WriteRecord(object record)
        {
            if (!Connected || _tokenSource.Token.IsCancellationRequested)
            {
                logs.Push(record);
                return;
            }
            //logs.Push(record);
            _ = WriteToServer(new object[1] { record });
        }

        private async Task WriteToServer(object[] records)
        {
            LogType log = new LogType
            {
                Items = records
            };

            try
            {
                await MessagePackSerializer.Typeless.SerializeAsync(client.GetStream(), log);
            }
            catch (Exception)
            {
                Console.WriteLine("Audit server connection failed");
                // Audit Server connection broken
                // Save logs for reconnect
                logs.PushRange(records);
                client.Close();
                client.Dispose();
            }
        }

        public async Task KeepConnected()
        {
            while (true)
            {
                try
                {
                    if (Connected)
                    {
                        Console.WriteLine("Audit Server Connection Healthy");
                        if (logs.Any())
                        {
                            var records = new object[logs.Count];
                            logs.TryPopRange(records, 0, records.Length);
                            _ = WriteToServer(records);
                        }
                        await Task.Delay(30000);
                        continue;
                    }
                    Console.WriteLine("Connecting to Audit Server");
                    client = new TcpClient(AddressFamily.InterNetwork);
#if DEBUG
                    await client.ConnectAsync(IPAddress.Loopback, Server.AUDIT_SERVER.Port).ConfigureAwait(false);
#else
                    await client.ConnectAsync(Server.AUDIT_SERVER.ServiceName, Server.AUDIT_SERVER.Port).ConfigureAwait(false);
#endif
                    if (!Connected)
                        await Task.Delay(5000);
                }
                catch (Exception)
                {
                    Console.WriteLine("Connection Failed");
                }
            }
        }
    }
}
