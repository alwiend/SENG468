using Base;
using Constants;
using Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace SellTriggerService
{
    public class CancelSetSell : BaseService
    {
        public CancelSetSell(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL(); 
                
                var triggerObject = await db.ExecuteAsync($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'").ConfigureAwait(false);
                if (triggerObject.Length == 0)
                {
                    return await LogErrorEvent(command, "Trigger does not exist").ConfigureAwait(false);
                }

                // Return stock to user account
                await db.ExecuteNonQueryAsync($"UPDATE stocks SET price=price+{triggerObject[0]["amount"]}" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'").ConfigureAwait(false);
                command.funds = Convert.ToDecimal(triggerObject[0]["amount"])/100m;
                // Remove trigger
                await db.ExecuteNonQueryAsync($"DELETE FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'").ConfigureAwait(false);

                result = "Trigger removed";
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
