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
                var userObject = await db.ExecuteAsync($"SELECT price FROM stocks" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'").ConfigureAwait(false);
                if (userObject.Length == 0)
                {
                    return await LogErrorEvent(command, "User does not exist").ConfigureAwait(false);
                }

                int balance = Convert.ToInt32(userObject[0]["price"]);
                // normalize
                int quantity = (int)(command.funds * 100);

                if (balance < quantity)
                {
                    return await LogErrorEvent(command, "Insufficient funds").ConfigureAwait(false);
                }

                // Check if there is already a trigger
                var triggerObject = await db.ExecuteAsync($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'").ConfigureAwait(false);
                if (triggerObject.Length > 0)
                {
                    return await LogErrorEvent(command, "Trigger already exists").ConfigureAwait(false);
                }

                // Set aside required stock
                balance -= quantity;
                await db.ExecuteNonQueryAsync($"UPDATE stocks SET price={balance}" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'").ConfigureAwait(false);

                // Update triggers with details
                await db.ExecuteNonQueryAsync($"INSERT INTO triggers (userid,stock,amount,triggerType) " +
                    $"VALUES ('{command.username}','{command.stockSymbol}',{quantity},'SELL')").ConfigureAwait(false);

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
