// Connects to Quote Server and returns quote
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Base;
using Constants;
using Utilities;
using StackExchange.Redis;

namespace QuoteService
{

    public class QuoteService : BaseService
    {
        public static async Task Main(string[] args)
        {
            var quote_service = new QuoteService(Service.QUOTE_SERVICE, new AuditWriter(), ConnectionMultiplexer.Connect("redis_quote_cache:6379"));
            await quote_service.StartService();
        }

        public QuoteService(ServiceConstant sc, IAuditWriter aw, ConnectionMultiplexer cm): base(sc, aw, cm)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            try
            {
                IDatabase redisConn = muxer.GetDatabase(1);
                string result = redisConn.StringGet(command.stockSymbol.ToUpper());
                if (result != null)
                {
                    string[] args = result.Split(",");
                    if ((Unix.TimeStamp - Convert.ToInt64(args[1])) < 60000)
                    {
                        if (command.command == commandType.SET_BUY_TRIGGER || command.command == commandType.SET_SELL_TRIGGER)
                        {
                            return result;
                        }
                        return args[0];
                    }                
                }
                string quote = await GetQuote(command, redisConn);
                if (command.command == commandType.SET_BUY_TRIGGER || command.command == commandType.SET_SELL_TRIGGER)
                {
                    return quote;
                }
                return quote.Split(",")[0];
            }
            catch (Exception ex)
            {
                return $"Error getting quote";
            }
        }

        async Task<string> GetQuote(UserCommandType command, IDatabase redisConn)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            var quote = await conn.Send($"{command.stockSymbol},{command.username}", true).ConfigureAwait(false);
            var cost = await LogQuoteServerEvent(command, quote).ConfigureAwait(false);

            var result = $"{cost},{Unix.TimeStamp}";
            redisConn.StringSet(command.stockSymbol.ToUpper(), result);
            return result;
        }
    }
}