// Connects to Quote Server and returns quote
using System;
using System.Threading.Tasks;
using Utilities;
using StackExchange.Redis;
using System.Threading;
using TransactionServer.Services.Quote;

namespace TransactionServer.Services
{

    public class QuoteService : BaseService
    {
        public static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(Environment.ProcessorCount * 15, Environment.ProcessorCount * 10);
            try
            {
#if DEBUG
                //var mult = await ConnectionMultiplexer.ConnectAsync("localhost:6379").ConfigureAwait(false);
#else
                //var mult = await ConnectionMultiplexer.ConnectAsync("redis_quote_cache:6379").ConfigureAwait(false);
#endif
                var quote_service = new QuoteService(Service.QUOTE_SERVICE, new AuditWriter());
                await quote_service.StartService();
                quote_service.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static readonly QuoteCache<string> _quoteCache = new QuoteCache<string>();

        public QuoteService(ServiceConstant sc, IAuditWriter aw, ConnectionMultiplexer cm) : base(sc, aw, cm)
        {
        }

        public QuoteService(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            try
            {
                //IDatabase redisConn = Muxer.GetDatabase(1);
                var result = await _quoteCache.GetOrCreate(command.stockSymbol,
                    () => GetQuote(command),
                    v => (Unix.TimeStamp - Convert.ToInt64(v.Split(",")[1])) < 60000);
                if (command.command != commandType.SET_BUY_TRIGGER && command.command != commandType.SET_SELL_TRIGGER)
                {
                    return result.Split(",")[0];
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return $"Error getting quote";
            }
        }

        async Task<string> GetQuote(UserCommandType command)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            var quote = await conn.Send($"{command.stockSymbol},{command.username}", true).ConfigureAwait(false);
            var cost = LogQuoteServerEvent(command, quote);

            var result = $"{cost},{Unix.TimeStamp}";
            //redisConn.StringSet(command.stockSymbol.ToUpper(), result);
            return result;
        }

        int LogQuoteServerEvent(UserCommandType command, string quote)
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
            Auditor.WriteRecord(stockQuote);
            return (int)(stockQuote.price * 100);
        }
    }
}