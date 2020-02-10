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
                action = "remove",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        string LogQuoteServerEvent(UserCommandType command, string quote)
        {
            //Cost,StockSymbol,UserId,Timestamp,CryptoKey
            string[] args = quote.Split(",");
            QuoteServerType stockQuote = new QuoteServerType()
            {
                username = args[2],
                server = Server.QUOTE_SERVER.Abbr,
                price = decimal.Parse(args[0]),
                transactionNum = command.transactionNum,
                stockSymbol = args[1],
                timestamp = Unix.TimeStamp.ToString(),
                quoteServerTime = args[3],
                cryptokey = args[4]
            };
            Auditor.WriteRecord(stockQuote);
            return stockQuote.price.ToString();
        }

        string LogUserErrorEvent(UserCommandType command)
        {
            ErrorEventType error = new ErrorEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                stockSymbol = command.stockSymbol,
                funds = command.funds,
                errorMessage = "User does not exist or user balance error"
            };
            Auditor.WriteRecord(error);
            return error.errorMessage;
        }

        string LogDBErrorEvent(UserCommandType command)
        {
            ErrorEventType error = new ErrorEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                stockSymbol = command.stockSymbol,
                funds = command.funds,
                errorMessage = "Error getting account details"
            };
            Auditor.WriteRecord(error);
            return error.errorMessage;
        }

        void LogDebugEvent(UserCommandType command, Exception e)
        {
            DebugType bug = new DebugType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                debugMessage = e.ToString()
            };
            Auditor.WriteRecord(bug);
        }

        public string BuyStock(UserCommandType command)
        {
            string result;
            string stockCost = GetStock(command);
            long balance;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length <= 0)
                {
                    return LogUserErrorEvent(command);
                }

                balance = long.Parse(userObject[0]["money"]) / 100; // normalize
                decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost)); // most whole number stock that can buy

                if (balance < command.funds)
                {
                    return LogUserErrorEvent(command);
                }
                if (numStock < 1)
                {
                    return LogUserErrorEvent(command);
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
                LogTransactionEvent(command);
                }
                catch (Exception e)
                {
                    result = LogDBErrorEvent(command);
                    LogDebugEvent(command, e);
                }
            return result;
        }

        string GetStock(UserCommandType command)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            string quote = conn.Send($"{command.stockSymbol},{command.username}", true);
            return LogQuoteServerEvent(command, quote);
        }
    }
}
