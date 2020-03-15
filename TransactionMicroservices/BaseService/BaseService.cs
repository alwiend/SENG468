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

namespace Base
{
    public abstract class BaseService
    {
        protected IAuditWriter Auditor { get; }
        protected ServiceConstant ServiceDetails { get; }

        protected ConnectionMultiplexer muxer { get; }

        public BaseService(ServiceConstant sc, IAuditWriter aw, ConnectionMultiplexer cm)
        {
            ServiceDetails = sc;
            Auditor = aw;
            muxer = cm;
        }

        public BaseService(ServiceConstant sc, IAuditWriter aw)
        {
            ServiceDetails = sc;
            Auditor = aw;
        }

        /*
		 * Logs interserver communication
		 * @param command The user command that is driving the process
		 */
        async Task LogServerEvent(UserCommandType command)
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
                sysEvent.funds = command.funds/100m;
            Auditor.WriteRecord(sysEvent).ConfigureAwait(false);
        }

        protected async Task LogTransactionEvent(UserCommandType command, string action)
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
            Auditor.WriteRecord(transaction).ConfigureAwait(false);
        }

        protected async Task<int> LogQuoteServerEvent(UserCommandType command, string quote)
        {
            //Cost,StockSymbol,UserId,Timestamp,CryptoKey
            string[] args = quote.Split(",");
            QuoteServerType stockQuote = new QuoteServerType()
            {
                username = args[2],
                server = Server.QUOTE_SERVER.Abbr,
                price = Convert.ToDecimal(args[0]),
                transactionNum = command.transactionNum,
                stockSymbol = args[1],
                timestamp = Unix.TimeStamp.ToString(),
                quoteServerTime = args[3],
                cryptokey = args[4]
            };
            Auditor.WriteRecord(stockQuote).ConfigureAwait(false);
            return (int)(stockQuote.price * 100);
        }

        protected async Task<string> LogErrorEvent(UserCommandType command, string err)
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

            Auditor.WriteRecord(error).ConfigureAwait(false);
            return error.errorMessage;
        }

        protected async Task LogDebugEvent(UserCommandType command, string err)
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
            Auditor.WriteRecord(bug).ConfigureAwait(false);
        }

        protected virtual Task<string> DataReceived(UserCommandType userCommand) { return null; }

        private async Task ProcessClient(TcpClient client)
        {
            UserCommandType command = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(UserCommandType));

                using StreamReader client_in = new StreamReader(client.GetStream());
                command = (UserCommandType)serializer.Deserialize(client_in);
                await LogServerEvent(command).ConfigureAwait(false);

                string retData = await DataReceived(command).ConfigureAwait(false);
                //string retData = command.command.ToString();

                using StreamWriter client_out = new StreamWriter(client.GetStream());
                await client_out.WriteAsync(retData).ConfigureAwait(false);
                await client_out.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (command != null) await LogDebugEvent(command, ex.Message);
                else Console.WriteLine(ex);
            }
            finally
            {
                client.Close();
            }
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
