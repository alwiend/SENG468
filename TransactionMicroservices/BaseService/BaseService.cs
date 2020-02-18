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

namespace Base
{
    public abstract class BaseService
    {
        protected IAuditWriter Auditor { get; }
        protected ServiceConstant ServiceDetails { get; }

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
                sysEvent.funds = command.funds;
            await Auditor.WriteRecord(sysEvent).ConfigureAwait(false);
        }

        protected async Task LogTransactionEvent(UserCommandType command, string action)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = action,
                username = command.username,
                funds = command.funds
            };
            await Auditor.WriteRecord(transaction).ConfigureAwait(false);
        }

        protected

        async Task<string> LogQuoteServerEvent(UserCommandType command, string quote)
        {
            //Cost,StockSymbol,UserId,Timestamp,CryptoKey
            string[] args = quote.Split(",");
            QuoteServerType stockQuote = new QuoteServerType()
            {
                username = args[2],
                server = Server.QUOTE_SERVER.Abbr,
                price = decimal.Parse(args[0]),
                transactionNum = command.transactionNum,
                stockSymbol = args[1],
                timestamp = Unix.TimeStamp.ToString(),
                quoteServerTime = args[3],
                cryptokey = args[4]
            };
            await Auditor.WriteRecord(stockQuote).ConfigureAwait(false);
            return stockQuote.price.ToString();
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
                funds = command.funds,
                errorMessage = err
            };
            await Auditor.WriteRecord(error).ConfigureAwait(false);
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
                debugMessage = err
            };
            await Auditor.WriteRecord(bug).ConfigureAwait(false);
        }

        protected virtual Task<string> DataReceived(UserCommandType userCommand) { return null; }

        private async Task ProcessClient(TcpClient client)
        {
            await Task.Run(async () =>
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(UserCommandType));

                    using StreamReader client_in = new StreamReader(client.GetStream());
                    UserCommandType command = (UserCommandType)serializer.Deserialize(client_in);
                    await LogServerEvent(command).ConfigureAwait(false);

                    string retData = await DataReceived(command).ConfigureAwait(false);

                    using StreamWriter client_out = new StreamWriter(client.GetStream());
                    await client_out.WriteAsync(retData).ConfigureAwait(false);
                    await client_out.FlushAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    client.Close();
                }
            }).ConfigureAwait(false);
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
                ProcessClient(client);
            }
        }
    }
}
