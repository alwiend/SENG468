// Connects to Quote Server and returns quote
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Base;
using Constants;
using Utilities;

namespace QuoteService
{

    public class QuoteService : BaseService
    {
        public static void Main(string[] args)
        {
            var quote_service = new QuoteService(Service.QUOTE_SERVICE, new AuditWriter());
            quote_service.StartService();
        }

        public QuoteService(ServiceConstant sc, IAuditWriter aw): base(sc, aw)
        {
            DataReceived = RequestQuote;
        }

        string LogQuoteServerEvent(UserCommandType command, string quote)
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
            Auditor.WriteRecord(stockQuote);
            return stockQuote.price.ToString();
        }

        string RequestQuote(UserCommandType command)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            var quote = conn.Send($"{command.stockSymbol},{command.username}", true);
            return LogQuoteServerEvent(command, quote);
        }
    }
}