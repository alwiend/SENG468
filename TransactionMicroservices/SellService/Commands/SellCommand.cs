using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Base;
using Database;
using Newtonsoft.Json;
using Utilities;
using Constants;
using System.Threading.Tasks;

namespace SellService
{
    class SellCommand : BaseService
    {
        public SellCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }


        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            string stockCost = await GetStock(command).ConfigureAwait(false);
            double stockBalance;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                var userObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObj.Length > 0)
                {
                    stockBalance = double.Parse(userObj[0]["price"]) / 100;
                    if (stockBalance < (double)command.funds)
                    {
                        return await LogErrorEvent(command, "Insufficient stocks").ConfigureAwait(false);
                    }
                    decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost));
                    double amount = Math.Round((double)numStock * double.Parse(stockCost), 2);
                    double leftoverStock = stockBalance - amount;
                    if (numStock < 1)
                    {
                        return await LogErrorEvent(command, "Insufficient stocks").ConfigureAwait(false);
                    }
                    db.ExecuteNonQuery($"INSERT INTO transactions (userid, stock, price, transType, transTime) VALUES ('{command.username}','{command.stockSymbol}',{amount*100},'SELL','{Unix.TimeStamp}')");
                    result = $"${amount} of stock {command.stockSymbol} is available to sell at ${stockCost} per share";
                    db.ExecuteNonQuery($"UPDATE stocks SET price={leftoverStock*100} WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                    command.funds = (decimal)amount;
                } else
                {
                    result = await LogErrorEvent(command, "User does not exist");
                }
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
