using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Base;
using Utilities;
using Constants;

namespace BuyService
{
    class BuyService
    {
        public static void Main(string[] args)
        {
            var buy_service = new BuyCommand(Service.BUY_SERVICE, new AuditWriter());
            var buy_commit_service = new BuyCommitCommand(Service.BUY_COMMIT_SERVICE, new AuditWriter()); 
            var buy_cancel_service = new BuyCancelCommand(Service.BUY_CANCEL_SERVICE, new AuditWriter());
            
            Thread buyThread = new Thread(new ThreadStart(() => buy_service.StartService()));
            Thread commitThread = new Thread(new ThreadStart(() => buy_commit_service.StartService()));
            Thread cancelThread = new Thread(new ThreadStart(() => buy_cancel_service.StartService()));

            buyThread.Start();
            commitThread.Start();
            cancelThread.Start();
        }
    }
}
