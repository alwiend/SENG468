using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace BuyTriggerService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var buy_trigger_amount_service = new SetBuyAmount(Constants.Service.BUY_TRIGGER_AMOUNT_SERVICE, _auditor);
            var buy_trigger_cancel_service = new CancelSetBuy(Constants.Service.BUY_TRIGGER_CANCEL_SERVICE, _auditor);
            var buy_trigger_set_service = new SetBuyTrigger(Constants.Service.BUY_TRIGGER_SET_SERVICE, _auditor);

            List<Task> tasks = new List<Task>
            {
                buy_trigger_amount_service.StartService(),
                buy_trigger_cancel_service.StartService(),
                buy_trigger_set_service.StartService()
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
