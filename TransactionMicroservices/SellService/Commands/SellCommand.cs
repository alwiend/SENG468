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
            SystemEventType transaction = new SystemEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                funds = command.funds,
                stockSymbol = command.stockSymbol
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

        object SellStock(UserCommandType command)
        {
            string result;
            string stockCost = GetStock(command);
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
                        return LogUserErrorEvent(command);
                    }
                    decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost));
                    double amount = Math.Round((double)numStock * double.Parse(stockCost), 2);
                    double leftoverStock = stockBalance - amount;
                    if (numStock < 1)
                    {
                        return LogUserErrorEvent(command);
                    }
                    db.ExecuteNonQuery($"INSERT INTO transactions (userid, stock, price, transType, transTime) VALUES ('{command.username}','{command.stockSymbol}',{amount*100},'SELL','{Unix.TimeStamp}')");
                    result = $"${amount} of stock {command.stockSymbol} is available to sell at ${stockCost} per share";
                    db.ExecuteNonQuery($"UPDATE stocks SET price={leftoverStock*100} WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                    command.funds = (decimal)amount;
                } else
                {
                    result = LogUserErrorEvent(command);
                }
            }
            catch (Exception e)
            {
                result = LogDBErrorEvent(command);
                LogDebugEvent(command, e);
            }
            LogTransactionEvent(command);
            return result;
        }

        public string GetStock(UserCommandType command)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            string quote = conn.Send($"{command.stockSymbol},{command.username}", true);
            return LogQuoteServerEvent(command, quote);
        }
    }
}
