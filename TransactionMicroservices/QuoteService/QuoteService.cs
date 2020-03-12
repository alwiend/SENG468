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
            var quote_service = new QuoteService(Service.QUOTE_SERVICE, new AuditWriter());
            await quote_service.StartService();
        }

        public QuoteService(ServiceConstant sc, IAuditWriter aw): base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            ConnectionMultiplexer muxer;
            try
            {
                // The address is the one for my docker toolbox (localhost)
                muxer = await ConnectionMultiplexer.ConnectAsync("192.168.99.100:6379").ConfigureAwait(false);
                //muxer = await ConnectionMultiplexer.ConnectAsync("localhost:6379").ConfigureAwait(false);
                //muxer = await ConnectionMultiplexer.ConnectAsync("172.0.0.1:6379").ConfigureAwait(false);
                IDatabase redisConn = muxer.GetDatabase(1);
                string result = redisConn.StringGet(command.stockSymbol.ToUpper());
                if (result == null)
                {
                    return await GetQuote(command, redisConn);
                }
                string[] args = result.Split(",");
                if ((Unix.TimeStamp - Convert.ToInt64(args[3])) > 60000)
                {
                    return await GetQuote(command, redisConn);
                }
                return args[0];
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
            redisConn.StringSet(command.stockSymbol.ToUpper(), quote);
            return await LogQuoteServerEvent(command, quote).ConfigureAwait(false);
        }
    }
}