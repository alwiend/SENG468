using Base;
using Constants;
using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace SellTriggerService
{
    public class SetSellAmount : BaseService
    {
        public SetSellAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
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
                    return await LogErrorEvent(command, "User does not exist").ConfigureAwait(false);
                }

                int balance = int.Parse(userObject[0]["price"]);
                // normalize
                int quantity = (int)(command.funds * 100);

                if (balance < quantity)
                {
                    return await LogErrorEvent(command, "Insufficient funds").ConfigureAwait(false);
                }

                // Check if there is already a trigger
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length > 0)
                {
                    return await LogErrorEvent(command, "Trigger already exists").ConfigureAwait(false);
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
                await LogDebugEvent(command, ex.Message).ConfigureAwait(false);
                return await LogErrorEvent(command, "Error processing command").ConfigureAwait(false);
            }
            return result;
        }
    }
}
