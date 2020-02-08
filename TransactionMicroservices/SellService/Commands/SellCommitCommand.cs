using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
using Constants;
using Utilities;

namespace SellService
{
    class SellCommitCommand : BaseService
    {
        public SellCommitCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = CommitSell;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "commit sell",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        object CommitSell(UserCommandType command)
        {
            string result;
            string stock;
            double amount;
            DateTime currTime = DateTime.UtcNow;
            try
            {
                MySQL db = new MySQL();
                var hasTrans = db.Execute($"SELECT stock, price, transTime FROM transactions WHERE userid='{command.username}' AND transType='SELL'");
                var transObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrans);
                double minTime = 60.1;
                int minTimeIndex = -1;
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
                    } 
                    else
                    {
                        if (timeDiff > 60)
                        {
                            var userQuery = db.Execute($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'");
                            var userObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(userQuery);
                            int newBalance = Convert.ToInt32(userObj[0]["price"]) + (int)amount;
                            db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='SELL'");
                            db.ExecuteNonQuery($"UPDATE stocks SET money={newBalance} WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'");
                            command.funds = newBalance / 100;
                        }
                    }
                }

                if (minTimeIndex >= 0)
                {
                    stock = transObj[minTimeIndex]["stock"];
                    amount = int.Parse(transObj[minTimeIndex]["price"]);
                    var hasUser = db.Execute($"SELECT money FROM user WHERE userid='{command.username}'");
                    var userObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                    db.ExecuteNonQuery($"UPDATE user SET money={int.Parse(userObj[0]["money"]) + amount} WHERE userid='{command.username}'");
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='SELL'");
                    result = $"Successfully sold ${amount/100} worth of {stock}";
                    // db.ExecuteNonQuery DELETE STOCK FROM STOCKS TABLE
                    command.funds = (decimal)(amount + int.Parse(userObj[0]["money"]))/100;
                } else
                {
                    result = "No recent transactions to sell";
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
