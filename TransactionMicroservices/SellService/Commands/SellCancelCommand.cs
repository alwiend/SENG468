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
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "cancel sell",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
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
                            var hasStock = db.Execute($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'");
                            var stockObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasStock);
                            db.ExecuteNonQuery($"UPDATE stocks SET price={int.Parse(stockObj[0]["price"]) + amount} WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'");
                            db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='SELL' AND stock='{transObj[0]["stock"]}'");
                        }
                    }
                }

                if (minTimeIndex >=0)
                {
                    amount = int.Parse(transObj[minTimeIndex]["price"]);
                    string stock = transObj[minTimeIndex]["stock"];
                    var hasStock = db.Execute($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{stock}'");
                    var stockObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasStock);
                    db.ExecuteNonQuery($"UPDATE stocks SET price={amount + long.Parse(stockObj[0]["price"])} WHERE userid='{command.username}' AND stock='{stock}'");
                    result = $"Successfully canceled most recent sell command. \n" +
                        $"You have ${amount / 100} of {stock} back into your account.";
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='SELL'");
                } else
                {
                    result = "No transactions to cancel.";
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
    }
}
