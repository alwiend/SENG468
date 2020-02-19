using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Base;
using Database;
using Newtonsoft.Json;
using Utilities;
using Constants;
using System.Threading.Tasks;

namespace BuyService
{
    class BuyCommand : BaseService
    {
        public BuyCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw) 
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            string stockCost = await GetStock(command).ConfigureAwait(false);
            long balance;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length <= 0)
                {
                    return await LogErrorEvent(command, "User does not exist or user balance error").ConfigureAwait(false);
                }

                balance = long.Parse(userObject[0]["money"]) / 100; // normalize
                decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost)); // most whole number stock that can buy

                if (balance < command.funds)
                {
                    return await LogErrorEvent(command, "User does not exist or user balance error").ConfigureAwait(false);
                }
                if (numStock < 1)
                {
                    return await LogErrorEvent(command, "User does not exist or user balance error").ConfigureAwait(false);
                }

                double amount = (double)numStock * double.Parse(stockCost); // amount to send to pending transactions
                double leftover = balance - amount;
                leftover *= 100; // Get rid of decimal
                amount *= 100; // Get rid of decimal

                // Store pending transaction
                db.ExecuteNonQuery($"INSERT INTO transactions (userid, stock, price, transType, transTime) VALUES ('{command.username}','{command.stockSymbol}',{amount},'BUY','{Unix.TimeStamp.ToString()}')");

                result = $"{numStock} stock is available for purchase at {stockCost} per share totalling {String.Format("{0:0.00}", amount/100)}.";

                // Update the amount in user account
                db.ExecuteNonQuery($"UPDATE user SET money = {leftover} WHERE userid='{command.username}'");

                command.funds = (decimal)amount/100;
                await LogTransactionEvent(command, "remove").ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    result = await LogErrorEvent(command, "Error getting account details").ConfigureAwait(false); 
                    await LogDebugEvent(command, e.Message).ConfigureAwait(false);
                }
            return result;
        }

        async Task<string> GetStock(UserCommandType command)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            string quote = await conn.Send($"{command.stockSymbol},{command.username}", true).ConfigureAwait(false);
            return await LogQuoteServerEvent(command, quote).ConfigureAwait(false);
        }
    }
}
