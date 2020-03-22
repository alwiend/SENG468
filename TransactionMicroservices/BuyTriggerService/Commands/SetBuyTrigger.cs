using System;

using Constants;
using Utilities;
using Database;
using System.Threading.Tasks;

namespace TransactionServer.Services.BuyTrigger
{
    public class SetBuyTrigger : BaseService
    {
        public SetBuyTrigger(ServiceConstant sc, AuditWriter aw) : base(sc,aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            try
            {
                command.fundsSpecified = true;
                var msg = await BuyTriggerTimer.StartOrUpdateTimer(command).ConfigureAwait(false);
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
