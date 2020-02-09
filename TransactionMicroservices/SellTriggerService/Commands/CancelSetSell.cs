using Base;
using Constants;
using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SellTriggerService
{
    public class CancelSetSell : BaseService
    {
        public CancelSetSell(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
            DataReceived = CancelTrigger;
        }

        private string CancelTrigger(UserCommandType command)
        {
            string result;
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL(); 
                
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length == 0)
                {
                    return "Trigger does not exist";
                }

                // Return stock to user account
                db.ExecuteNonQuery($"UPDATE stocks SET price=price+{triggerObject[0]["amount"]}" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");

                // Remove trigger
                db.ExecuteNonQuery($"DELETE FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");

                result = "Trigger removed";
            }
            catch (Exception ex)
            {
                return "Error processing command";
            }

            return result;
        }
    }
}
