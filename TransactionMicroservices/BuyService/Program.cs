using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace BuyService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var buy_service = new BuyCommand(Constants.Service.BUY_SERVICE, _auditor);
            var buy_cancel_service = new BuyCancelCommand(Constants.Service.BUY_CANCEL_SERVICE, _auditor);
            var buy_commit_service = new BuyCommitCommand(Constants.Service.BUY_COMMIT_SERVICE, _auditor);

            List<Task> tasks = new List<Task>
            {
                buy_service.StartService(),
                buy_cancel_service.StartService(),
                buy_commit_service.StartService()
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
