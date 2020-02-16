using System;
using System.Threading;
using Utilities;

namespace BuyTriggerService
{
    class Program
    {
        static void Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var buy_trigger_amount_service = new SetBuyAmount(Constants.Service.BUY_TRIGGER_AMOUNT_SERVICE, _auditor);
            var buy_trigger_cancel_service = new CancelSetBuy(Constants.Service.BUY_TRIGGER_CANCEL_SERVICE, _auditor);
            var buy_trigger_set_service = new SetBuyTrigger(Constants.Service.BUY_TRIGGER_SET_SERVICE, _auditor);

            var buy_trigger_amount_thread = new Thread(new ThreadStart(buy_trigger_amount_service.StartService));
            var buy_trigger_cancel_thread = new Thread(new ThreadStart(buy_trigger_cancel_service.StartService));
            var buy_trigger_set_thread = new Thread(new ThreadStart(buy_trigger_set_service.StartService));

            buy_trigger_amount_thread.Start();
            buy_trigger_cancel_thread.Start();
            buy_trigger_set_thread.Start();

            buy_trigger_amount_thread.Join();
            buy_trigger_cancel_thread.Join();
            buy_trigger_set_thread.Join();
        }
    }
}
