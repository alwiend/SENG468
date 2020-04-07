using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TransactionServer.Services.SellTrigger;
using Utilities;

namespace TransactionServer.Services
{
    public static class SellTriggerService
    {
        public static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(Environment.ProcessorCount * 15, Environment.ProcessorCount * 10);
            var _auditor = new AuditWriter();
            var services = new List<BaseService>()
            {
                new SetSellAmount(Service.SELL_TRIGGER_AMOUNT_SERVICE, _auditor),
                new CancelSetSell(Service.SELL_TRIGGER_CANCEL_SERVICE, _auditor),
                new SetSellTrigger(Service.SELL_TRIGGER_SET_SERVICE, _auditor)
            };

            var tasks = services.Select(service => service.StartService());

            await Task.WhenAll(tasks).ConfigureAwait(false);
            _auditor.Dispose();
            services.ForEach(task => task.Dispose());
        }
    }
}
