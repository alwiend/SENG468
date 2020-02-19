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
    public class SetBuyAmount : BaseService
    {
        public SetBuyAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result = "";
            try
            {
                // Check user account for cash, notify if insufficient funds
                MySQL db = new MySQL();
                var userObject = await db.ExecuteAsync($"SELECT userid, money FROM user WHERE userid='{command.username}'").ConfigureAwait(false);

                if (userObject.Length <= 0)
                {
                    return await LogErrorEvent(command, "User does not exist").ConfigureAwait(false);
                }

                decimal balance =Convert.ToDecimal(userObject[0]["money"]) / 100.0m; // normalize

                if (balance < command.funds)
                {
                    return await LogErrorEvent(command, "Insufficeint funds");
                }

                // Check if there is already a trigger
                var triggerObject = await db.ExecuteAsync($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'").ConfigureAwait(false);
                if (triggerObject.Length > 0)
                {
                    return await LogErrorEvent(command, "Trigger already exists").ConfigureAwait(false);
                }

                // Set aside required cash
                balance -= command.funds;
                await db.ExecuteNonQueryAsync($"UPDATE user SET money={(long)(balance*100)} WHERE userid='{command.username}'").ConfigureAwait(false);

                // Update triggers with details
                await db.ExecuteNonQueryAsync($"INSERT INTO triggers (userid,stock,amount,triggerType) " +
                    $"VALUES ('{command.username}','{command.stockSymbol}',{(long)(command.funds*100)},'BUY')").ConfigureAwait(false);

                result = "Trigger Created";
                await LogTransactionEvent(command, "remove").ConfigureAwait(false);
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
