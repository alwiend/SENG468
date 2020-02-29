using Base;
using Constants;
using Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace BuyTriggerService
{
    public class CancelSetBuy : BaseService
    {
        public CancelSetBuy(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result = "";
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL(); 
                
                var triggerObject = await db.ExecuteAsync($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'").ConfigureAwait(false);
                if (triggerObject.Length <= 0)
                {
                    return await LogErrorEvent(command, "Trigger does not exist").ConfigureAwait(false);
                }

                // Update user account
                await db.ExecuteNonQueryAsync($"UPDATE user SET money=money+{triggerObject[0]["amount"]} WHERE userid='{command.username}'").ConfigureAwait(false);
                command.funds = Convert.ToDecimal(triggerObject[0]["amount"]) / 100;
                // Remove trigger
                await db.ExecuteNonQueryAsync($"DELETE FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'").ConfigureAwait(false);

                result = "Trigger removed";
                await LogTransactionEvent(command, "add").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await LogDebugEvent(command, ex.Message).ConfigureAwait(false);
                return await LogErrorEvent(command, "Error processing command").ConfigureAwait(false);
            }
            return result;
        }
    }
}
