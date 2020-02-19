using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Base;
using Database;
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
                var userObj = await db.ExecuteAsync($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{command.stockSymbol}'").ConfigureAwait(false);
                if (userObj.Length > 0)
                {
                    stockBalance = Convert.ToDouble(userObj[0]["price"]) / 100;
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
                    await db.ExecuteNonQueryAsync($"INSERT INTO transactions (userid, stock, price, transType, transTime) " +
                        $"VALUES ('{command.username}','{command.stockSymbol}',{amount*100},'SELL','{Unix.TimeStamp}')").ConfigureAwait(false);
                    result = $"${amount} of stock {command.stockSymbol} is available to sell at ${stockCost} per share";
                    await db.ExecuteNonQueryAsync($"UPDATE stocks SET price={leftoverStock*100} " +
                        $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}'").ConfigureAwait(false);
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
