using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;
using TransactionServer.Services.Buy;
using System.Linq;

namespace TransactionServer.Services
{
    public class BuyService
    {
        public static async Task Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var services = new List<BaseService>()
            {
                new BuyCommand(Constants.Service.BUY_SERVICE, _auditor),
                new BuyCancelCommand(Constants.Service.BUY_CANCEL_SERVICE, _auditor),
                new BuyCommitCommand(Constants.Service.BUY_COMMIT_SERVICE, _auditor)
            };

            var tasks = services.Select(service => service.StartService());

            await Task.WhenAll(tasks).ConfigureAwait(false);
            _auditor.Dispose();
            services.ForEach(task => task.Dispose());
        }
    }
}
