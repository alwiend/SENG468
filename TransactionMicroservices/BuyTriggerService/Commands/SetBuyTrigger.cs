using System;
using System.Collections.Generic;
using System.Text;

using Constants;
using Base;
using Utilities;
using Database;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BuyTriggerService
{
    public class SetBuyTrigger : BaseService
    {
        public SetBuyTrigger(ServiceConstant sc, AuditWriter aw) : base(sc,aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL();

                var hasTrigger = db.Execute($"SELECT amount,triggerAmount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length == 0)
                {
                    return await LogErrorEvent(command, "Trigger does not exist.").ConfigureAwait(false);
                }

                if (Convert.ToInt32(triggerObject[0]["triggerAmount"])/100m == command.funds)
                {
                    return await LogErrorEvent(command, "Trigger is already set.").ConfigureAwait(false);
                }

                // Remove trigger
                db.ExecuteNonQuery($"UPDATE triggers " +
                    $"SET triggerAmount={command.funds*100}" +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");

                result = "Trigger amount set";
                await LogTransactionEvent(command, "remove").ConfigureAwait(false);
                decimal amount = Convert.ToDecimal(triggerObject[0]["amount"]) / 100m;
                var timer = new BuyTriggerTimer(command.username, command.stockSymbol, amount, command.funds);
                timer.Start();
            }
            catch (Exception ex)
            {
                await LogDebugEvent(command, ex.Message).ConfigureAwait(false);
                return await LogErrorEvent(command, "Error processing command.").ConfigureAwait(false);
            }
            return result;
        }
    }
}
