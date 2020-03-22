// A C# Program for Server 
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using Constants;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using StackExchange.Redis;
using MessagePack;
using System.Buffers;

namespace TransactionServer.Services
{
    public abstract class BaseService : IDisposable
    {
        protected IAuditWriter Auditor { get; }
        protected ServiceConstant ServiceDetails { get; }

        protected ConnectionMultiplexer Muxer { get; }
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public BaseService(ServiceConstant sc, IAuditWriter aw, ConnectionMultiplexer cm)
        {
            ServiceDetails = sc;
            Auditor = aw;
            Muxer = cm;
        }

        public BaseService(ServiceConstant sc, IAuditWriter aw)
        {
            ServiceDetails = sc;
            Auditor = aw;
        }

        public virtual void Dispose()
        {
            Auditor.Dispose();
            Muxer.Dispose();
            _tokenSource.Dispose();
        }

        /*
		 * Logs interserver communication
		 * @param command The user command that is driving the process
		 */
        protected void LogServerEvent(UserCommandType command)
        {
            SystemEventType sysEvent = new SystemEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                username = command.username,
                fundsSpecified = command.fundsSpecified,
                command = command.command,
                filename = command.filename,
                stockSymbol = command.stockSymbol
            };
            if (command.fundsSpecified)
                sysEvent.funds = command.funds / 100m;
            Auditor.WriteRecord(sysEvent);
        }

        protected void LogTransactionEvent(UserCommandType command, string action)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = action,
                username = command.username
            };
            if (command.fundsSpecified)
                transaction.funds = command.funds / 100m;
            Auditor.WriteRecord(transaction);
        }


        protected string LogErrorEvent(UserCommandType command, string err)
        {
            ErrorEventType error = new ErrorEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                stockSymbol = command.stockSymbol,
                fundsSpecified = command.fundsSpecified,
                errorMessage = err
            };
            if (command.fundsSpecified)
                error.funds = command.funds / 100m;

            Auditor.WriteRecord(error);
            return error.errorMessage;
        }

        protected void LogDebugEvent(UserCommandType command, string err)
        {
            DebugType bug = new DebugType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                debugMessage = err,
                fundsSpecified = command.fundsSpecified
            };
            if (command.fundsSpecified)
                bug.funds = command.funds / 100m;
            Auditor.WriteRecord(bug);
        }

        protected virtual Task<string> DataReceived(UserCommandType userCommand) { return null; }

        protected virtual async Task ProcessClient(TcpClient client)
        {
            UserCommandType command;
            ReadOnlySequence<byte>? msgpack;
            ServiceResult sr = new ServiceResult();
            var streamReader = new MessagePackStreamReader(client.GetStream());
            while (client.Connected)
            {
                command = null;
                try
                {
                    msgpack = await streamReader.ReadAsync(_tokenSource.Token).ConfigureAwait(false);
                    if (!msgpack.HasValue)
                    {
                        break; // End of connection
                    }
                    command = MessagePackSerializer.Deserialize<UserCommandType>(msgpack.Value, cancellationToken: _tokenSource.Token);

                    if (command == null)
                    {
                        continue;
                    }

                    LogServerEvent(command);
                    sr.Message = await DataReceived(command).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(sr.Message))
                    {
                        continue;
                    }
                    await MessagePackSerializer.SerializeAsync<ServiceResult>(client.GetStream(), sr).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // Client connection failed. Dispose of it
                    break;
                }
                catch (Exception e)
                {
                    if (command != null) LogDebugEvent(command, e.Message);
                    else Console.WriteLine(e);
                    Console.WriteLine(e);
                }
            }
            streamReader.Dispose();
            client.Dispose();
        }

        public async Task StartService()
        {
            IPAddress ipAddr = IPAddress.Any;
            IPEndPoint _localEndPoint = new IPEndPoint(ipAddr, ServiceDetails.Port);

            TcpListener _listener = new TcpListener(_localEndPoint);
            _listener.Start();
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = ProcessClient(client);
            }
        }
    }
}
