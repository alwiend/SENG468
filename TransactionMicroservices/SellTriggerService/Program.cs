using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace SellTriggerService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var sell_trigger_amount_service = new SetSellAmount(Constants.Service.SELL_TRIGGER_AMOUNT_SERVICE, _auditor);
            var sell_trigger_cancel_service = new CancelSetSell(Constants.Service.SELL_TRIGGER_CANCEL_SERVICE, _auditor);
            var sell_trigger_set_service = new SetSellTrigger(Constants.Service.SELL_TRIGGER_SET_SERVICE, _auditor);

            List<Task> tasks = new List<Task>
            {
                sell_trigger_amount_service.StartService(),
                sell_trigger_cancel_service.StartService(),
                sell_trigger_set_service.StartService()
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
