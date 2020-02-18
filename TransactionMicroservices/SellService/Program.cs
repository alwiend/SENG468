using System;
using System.Threading;
using Utilities;
using Constants;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SellService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sell_service = new SellCommand(Service.SELL_SERVICE, new AuditWriter());
            var sell_commit_service = new SellCommitCommand(Service.SELL_COMMIT_SERVICE, new AuditWriter());
            var sell_cancel_service = new SellCancelCommand(Service.SELL_CANCEL_SERVICE, new AuditWriter());

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
