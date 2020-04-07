using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;
using TransactionServer.Services.Buy;
using System.Linq;
using System;
using System.Threading;

namespace TransactionServer.Services
{
    public class BuyService
    {
        public static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(Environment.ProcessorCount * 15, Environment.ProcessorCount * 10);
            var _auditor = new AuditWriter();
            var services = new List<BaseService>()
            {
                new BuyCommand(Service.BUY_SERVICE, _auditor),
                new BuyCancelCommand(Service.BUY_CANCEL_SERVICE, _auditor),
                new BuyCommitCommand(Service.BUY_COMMIT_SERVICE, _auditor)
            };

            var tasks = services.Select(service => service.StartService());

            await Task.WhenAll(tasks).ConfigureAwait(false);
            _auditor.Dispose();
            services.ForEach(task => task.Dispose());
        }
    }
}
