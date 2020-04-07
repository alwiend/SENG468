using System.Collections.Generic;
using System.Threading.Tasks;
using TransactionServer.Services;

namespace ProjectDebugger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<Task> services = new List<Task>
            {
                AddService.Main(args),
                BuyService.Main(args),
                BuyTriggerService.Main(args),
                DisplaySummaryService.Main(args),
                QuoteService.Main(args),
                SellService.Main(args),
                SellTriggerService.Main(args)
            };

            await Task.WhenAll(services);
        }
    }
}
