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

namespace SellService
{
    class SellCommand : BaseService
    {
        public SellCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = SellStock;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "sell",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        object SellStock(UserCommandType command)
        {
            string result;
            string stockCost = GetStock(command.stockSymbol, command.username);
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
                        return $"Insufficient amount of stock {command.stockSymbol} for this transaction";
                    }
                    decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost));
                    double amount = Math.Round((double)numStock * double.Parse(stockCost), 2);
                    double leftoverStock = stockBalance - amount;

                    var hasMoney = db.Execute($"SELECT money FROM user WHERE userid='{command.username}'");
                    var moneyObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasMoney);
                    db.ExecuteNonQuery($"INSERT INTO transactions (userid, stock, price, transType, transTime) VALUES ('{command.username}','{command.stockSymbol}',{amount*100},'SELL','{Unix.TimeStamp}')");
                    result = $"${amount} of stock {command.stockSymbol} is available to sell at ${stockCost} per share";
                    db.ExecuteNonQuery($"UPDATE stocks SET price={leftoverStock*100} WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                } else
                {
                    result = $"User {command.username} does not own any {command.stockSymbol} stock";
                }
            }
            catch (Exception e)
            {
                result = $"Error occured getting account details.";
                Console.WriteLine(e.ToString());
            }
            LogTransactionEvent(command);
            return result;
        }

        public string GetStock(string stockSymbol, string username)
        {

            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            string quote = conn.Send($"{stockSymbol},{username}", true);
            return quote.Split(",")[0];
        }
    }
}
