using Constants;
using MessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuditServer
{
    public class AuditClient : IDisposable
    {
        TcpClient client;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public AuditClient(TcpClient c)
        {
            client = c;
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            client.Dispose();
        }

        public async Task Process()
        {
            UserCommandType command;
            LogType log_in = null;
            ReadOnlySequence<byte>? msgpack;
            using (var streamReader = new MessagePackStreamReader(client.GetStream()))
            {
                while (client.Connected)
                {
                    command = null;
                    try
                    {
                        msgpack = await streamReader.ReadAsync(_tokenSource.Token);
                        if (!msgpack.HasValue)
                        {
                            if (!_tokenSource.IsCancellationRequested)
                            {
                                // Bad state, leave
                                break;
                            }
                        }
                        log_in = MessagePackSerializer.Typeless.Deserialize(msgpack.Value, cancellationToken: _tokenSource.Token) as LogType;
                        if (log_in == null)
                        {
                            continue;
                        }

                        for (int i = log_in.Items.Length - 1; i >= 0; i--)
                        {
                            var record = log_in.Items[i];
                            AuditServer.AddRecord(record);
                            if (record.GetType() == typeof(UserCommandType))
                            {
                                command = (UserCommandType)record;
                                if (command.command == commandType.DUMPLOG)
                                {
                                    AuditServer.DumpLog(command.filename);
                                }
                            }
                        }
                    }
                    catch (IOException)
                    {
                        // Client connection closed, eat and move on
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        DebugType debugEvent = new DebugType
                        {
                            server = Server.AUDIT_SERVER.Abbr,
                            debugMessage = e.Message
                        };
                        if (command != null)
                        {
                            debugEvent.command = command.command;
                            debugEvent.transactionNum = command.transactionNum;
                            debugEvent.username = command.username;
                        }
                        AuditServer.AddRecord(debugEvent);
                    }
                }
            }

            client.Close();
            client.Dispose();
        }
    }
}