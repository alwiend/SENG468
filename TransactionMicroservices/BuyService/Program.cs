using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Utilities;

namespace BuyService
{
    public class Program
    {


        public static void Main(string[] args)
        {
            var _auditor = new AuditWriter();
            var buy_service = new BuyCommand(Constants.Service.BUY_SERVICE, _auditor);
            var buy_cancel_service = new BuyCancelCommand(Constants.Service.BUY_CANCEL_SERVICE, _auditor);
            var buy_commit_service = new BuyCommitCommand(Constants.Service.BUY_COMMIT_SERVICE, _auditor);

            var buy_thread = new Thread(new ThreadStart(buy_service.StartService));
            var buy_cancel_thread = new Thread(new ThreadStart(buy_cancel_service.StartService));
            var buy_commit_thread = new Thread(new ThreadStart(buy_commit_service.StartService));

            buy_thread.Start();
            buy_cancel_thread.Start();
            buy_commit_thread.Start();

            buy_thread.Join();
            buy_cancel_thread.Join();
            buy_commit_thread.Join();
        }
    }
}
