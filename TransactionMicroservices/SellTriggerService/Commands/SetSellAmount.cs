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
    public class SetSellAmount : BaseService
    {
        public SetSellAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
            DataReceived = SetAmount;
        }

        private string SetAmount(UserCommandType command)
        {
            string result = "";
            try
            {
                // Check user account for stock, notify if insufficient
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT price FROM stocks" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length == 0)
                {
                    return "You do not own this stock.";
                }

                int balance = int.Parse(userObject[0]["price"]);
                // normalize
                int quantity = (int)(command.funds * 100);

                if (balance < quantity)
                {
                    return $"Insufficient stocks.";
                }

                // Check if there is already a trigger
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length > 0)
                {
                    return "Trigger already exists";
                }

                // Set aside required stock
                balance -= quantity;
                db.ExecuteNonQuery($"UPDATE stocks SET price={balance}" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");

                // Update triggers with details
                db.ExecuteNonQuery($"INSERT INTO triggers (userid,stock,amount,triggerType) " +
                    $"VALUES ('{command.username}','{command.stockSymbol}',{quantity},'SELL')");

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
