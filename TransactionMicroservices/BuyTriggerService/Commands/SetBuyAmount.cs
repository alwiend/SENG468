using Base;
using Constants;
using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace BuyTriggerService
{
    public class SetBuyAmount : BaseService
    {
        public SetBuyAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
            DataReceived = SetAmount;
        }

        private string SetAmount(UserCommandType command)
        {
            string result = "";
            try
            {
                // Check user account for cash, notify if insufficient funds
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length <= 0)
                {
                    return "User does not exist";
                }

                decimal balance = decimal.Parse(userObject[0]["money"]) / 100.0m; // normalize

                if (balance < command.funds)
                {
                    return $"Not enough money to set trigger.";
                }

                // Check if there is already a trigger
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length > 0)
                {
                    return "Trigger already exists";
                }

                // Set aside required cash
                balance -= command.funds;
                db.ExecuteNonQuery($"UPDATE user SET money={(long)(balance*100)} WHERE userid='{command.username}'");

                // Update triggers with details
                db.ExecuteNonQuery($"INSERT INTO triggers (userid,stock,amount,triggerType) " +
                    $"VALUES ('{command.username}','{command.stockSymbol}',{(long)(command.funds*100)},'BUY')");

                result = "Trigger Created";
            }
            catch (Exception ex)
            {
                return "Error processing command";
            }

            return result;
        }
    }
}
