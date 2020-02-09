using System;
using System.Threading;
using Utilities;

namespace SellTriggerService
{
    class Program
    {
        static void Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var sell_trigger_amount_service = new SetSellAmount(Constants.Service.SELL_TRIGGER_AMOUNT_SERVICE, _auditor);
            var sell_trigger_cancel_service = new CancelSetSell(Constants.Service.SELL_TRIGGER_CANCEL_SERVICE, _auditor);
            var sell_trigger_set_service = new SetSellTrigger(Constants.Service.SELL_TRIGGER_SET_SERVICE, _auditor);

            var sell_trigger_amount_thread = new Thread(new ThreadStart(sell_trigger_amount_service.StartService));
            var sell_trigger_cancel_thread = new Thread(new ThreadStart(sell_trigger_cancel_service.StartService));
            var sell_trigger_set_thread = new Thread(new ThreadStart(sell_trigger_set_service.StartService));

            sell_trigger_amount_thread.Start();
            sell_trigger_cancel_thread.Start();
            sell_trigger_set_thread.Start();

            sell_trigger_amount_thread.Join();
            sell_trigger_cancel_thread.Join();
            sell_trigger_set_thread.Join();
        }
    }
}
