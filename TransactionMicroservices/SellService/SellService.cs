using Utilities;
using System.Threading.Tasks;
using System.Collections.Generic;
using TransactionServer.Services.Sell;
using System.Threading;
using System;

namespace TransactionServer.Services
{
    public class SellService
    {
        public static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(Environment.ProcessorCount * 15, Environment.ProcessorCount * 10);
            var _auditor = new AuditWriter();
            var sell_service = new SellCommand(Service.SELL_SERVICE, _auditor);
            var sell_commit_service = new SellCommitCommand(Service.SELL_COMMIT_SERVICE, _auditor);
            var sell_cancel_service = new SellCancelCommand(Service.SELL_CANCEL_SERVICE, _auditor);

            List<Task> tasks = new List<Task>
            {
                sell_service.StartService(),
                sell_commit_service.StartService(),
                sell_cancel_service.StartService()
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
