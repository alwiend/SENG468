using System;

using Utilities;
using System.Threading.Tasks;

namespace TransactionServer.Services.SellTrigger
{
    public class SetSellTrigger : BaseService
    {
        public SetSellTrigger(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            try
            {
                command.fundsSpecified = true;
                var msg = await SellTriggerTimer.StartOrUpdateTimer(command).ConfigureAwait(false);
                if (msg == null)
                {
                    return $"Trigger amount set successfully for stock {command.stockSymbol}";
                }
                return LogErrorEvent(command, msg);
            }
            catch (Exception ex)
            {
                LogDebugEvent(command, ex.Message);
                return LogErrorEvent(command, "Error processing command.");
            }
        }
    }
}
