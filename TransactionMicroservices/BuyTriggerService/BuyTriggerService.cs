using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TransactionServer.Services.BuyTrigger;
using Utilities;

namespace TransactionServer.Services
{
    public class BuyTriggerService
    {
        public static async Task Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var services = new List<BaseService>()
            {
                new SetBuyAmount(Constants.Service.BUY_TRIGGER_AMOUNT_SERVICE, _auditor),
                new CancelSetBuy(Constants.Service.BUY_TRIGGER_CANCEL_SERVICE, _auditor),
                new SetBuyTrigger(Constants.Service.BUY_TRIGGER_SET_SERVICE, _auditor)
            };

            var tasks = services.Select(service => service.StartService());

            await Task.WhenAll(tasks).ConfigureAwait(false);
            _auditor.Dispose();
            services.ForEach(task => task.Dispose());
        }
    }
}
