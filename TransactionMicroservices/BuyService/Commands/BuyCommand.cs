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

namespace BuyService
{
    class BuyCommand : BaseService
    {
        public BuyCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw) 
        {
            DataReceived = BuyStock;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "buy",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        public string BuyStock(UserCommandType command)
        {
            string result;
            string stockCost = GetStock(command.stockSymbol, command.username);
            long balance;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length <= 0)
                {
                    return "Insufficient balance";
                }

                balance = long.Parse(userObject[0]["money"]) / 100; // normalize
                decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost)); // most whole number stock that can buy

                if (balance < command.funds)
                {
                    return $"Not enough money to buy {command.funds} worth of {command.stockSymbol}.";
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

                command.funds = (long)leftover;
                LogTransactionEvent(command);
                }
                catch (Exception e)
                {
                    result = $"Error occured getting account details.";
                    Console.WriteLine(e.ToString());
                }
            return result;
        }

        //public static void CacheBuyRequest(double amount, string stock, string stockCost, string user)
        //{
        //    string transaction = $"{user},{stock},{stockCost},{Convert.ToString(amount)}";
        //    BuyCache.StoreItemsInCache(user, transaction);
        //}

        public static string GetStock(string stockSymbol, string username)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            string quote = conn.Send($"{stockSymbol},{username}", true);
            return quote.Split(",")[0];
        }
    }
}
