using Base;
using Constants;
using Database;
using Newtonsoft.Json;
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
                
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length <= 0)
                {
                    return await LogErrorEvent(command, "Trigger does not exist").ConfigureAwait(false);
                }

                // Update user account
                db.ExecuteNonQuery($"UPDATE user SET money=money+{triggerObject[0]["amount"]} WHERE userid='{command.username}'");
                command.funds = decimal.Parse(triggerObject[0]["amount"]) / 100;
                // Remove trigger
                db.ExecuteNonQuery($"DELETE FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");

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
