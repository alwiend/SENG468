using System;
using System.Threading;
using Utilities;
using Constants;

namespace SellService
{
    class Program
    {
        static void Main(string[] args)
        {
            var sell_service = new SellCommand(Service.SELL_SERVICE, new AuditWriter());
            var sell_commit_service = new SellCommitCommand(Service.SELL_COMMIT_SERVICE, new AuditWriter());
            var sell_cancel_service = new SellCancelCommand(Service.SELL_CANCEL_SERVICE, new AuditWriter());

            Thread sellThread = new Thread(new ThreadStart(() => sell_service.StartService()));
            Thread sellCommitThread = new Thread(new ThreadStart(() => sell_commit_service.StartService()));
            Thread sellCancelThread = new Thread(new ThreadStart(() => sell_cancel_service.StartService()));

            sellThread.Start();
            sellCommitThread.Start();
            sellCancelThread.Start();
        }
    }
}
