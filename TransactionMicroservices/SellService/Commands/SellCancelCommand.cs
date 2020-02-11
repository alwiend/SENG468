using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
using Utilities;
using Constants;

namespace SellService
{
    class SellCancelCommand : BaseService
    {
        public SellCancelCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = CancelSell;
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

        string LogUserErrorEvent(UserCommandType command)
        {
            ErrorEventType error = new ErrorEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                errorMessage = "No recent transactions to cancel."
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

        object CancelSell(UserCommandType command)
        {
            string result;
            double amount;
            DateTime currTime = DateTime.UtcNow;
            try
            {
                MySQL db = new MySQL();
                var hasTrans = db.Execute($"SELECT stock, price, transTime FROM transactions WHERE userid='{command.username}' AND transType='SELL'");
                var transObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrans);
                int minTimeIndex = -1;
                double minTime = 60.1;
                command.funds = 0;
                for (int i = 0; i < transObj.Length; i++)
                {
                    amount = double.Parse(transObj[i]["price"]);
                    long tTime = long.Parse(transObj[i]["transTime"]);
                    DateTimeOffset transTime = DateTimeOffset.FromUnixTimeSeconds(tTime / 1000);
                    double timeDiff = (currTime - transTime).TotalSeconds;
                    if (timeDiff < minTime)
                    {
                        minTime = timeDiff;
                        minTimeIndex = i;
                    } else
                    {
                        if (timeDiff > 60)
                        {
                            db.ExecuteNonQuery($"UPDATE stocks SET price=price+{amount} WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'");
                            db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='SELL' AND stock='{transObj[0]["stock"]}'");
                        }
                    }
                }

                if (minTimeIndex >=0)
                {
                    amount = int.Parse(transObj[minTimeIndex]["price"]);
                    string stock = transObj[minTimeIndex]["stock"];
                    string tTime = transObj[minTimeIndex]["transTime"];
                    db.ExecuteNonQuery($"UPDATE stocks SET price=price+{amount} WHERE userid='{command.username}' AND stock='{stock}'");
                    result = $"Successfully canceled most recent sell command. \n" +
                        $"You have ${amount / 100} of {stock} back into your account.";
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='SELL' AND transTime='{tTime}'");
                    command.funds = (decimal)(amount / 100);
                    command.stockSymbol = stock;
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
    }
}
