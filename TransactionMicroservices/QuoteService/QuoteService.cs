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
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            var quote = await conn.Send($"{command.stockSymbol},{command.username}", true).ConfigureAwait(false);
            return await LogQuoteServerEvent(command, quote).ConfigureAwait(false);
        }
    }
}