using System;
using System.Collections.Generic;
using System.Text;

using Constants;
using Base;
using Utilities;
using Database;
using Newtonsoft.Json;

namespace SellTriggerService
{
    public class SetSellTrigger : BaseService
    {
        public SetSellTrigger(ServiceConstant sc, AuditWriter aw) : base(sc,aw)
        {
            DataReceived = SetTrigger;
        }

        private string SetTrigger(UserCommandType command)
        {
            string result;
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL();

                var hasTrigger = db.Execute($"SELECT amount,triggerAmount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length == 0)
                {
                    return "Trigger does not exist";
                }

                if (Convert.ToInt32(triggerObject[0]["triggerAmount"])/100m == command.funds)
                {
                    return "Trigger already set";
                }

                
                db.ExecuteNonQuery($"UPDATE triggers " +
                    $"SET triggerAmount={command.funds*100}" +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");

                result = "Trigger amount set";
                decimal amount = Convert.ToDecimal(triggerObject[0]["amount"]) / 100m;
                var timer = new SellTriggerTimer(command.username, command.stockSymbol, amount, command.funds);
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Error processing command";
            }

            return result;
        }
    }
}
