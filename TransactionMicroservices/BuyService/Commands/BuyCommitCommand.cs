using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
using Constants;
using Utilities;

namespace BuyService
{
    class BuyCommitCommand : BaseService
    {
        public BuyCommitCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = CommitBuy;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "commit buy",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        public string CommitBuy(UserCommandType command)
        {
            string result;
            string stock = "";
            double amount = 0;
            DateTime currTime = DateTime.UtcNow;
            try
            {
                MySQL db = new MySQL();
                var queryRes = db.Execute($"SELECT stock, price, transTime FROM transactions WHERE userid='{command.username}' AND transType='BUY'");
                var transObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(queryRes);
                double minTime = 60.1;
                int minTimeIndex = -1;
                for (int i = 0; i < transObj.Length; i++)
                {
                    stock = transObj[i]["stock"];
                    amount = double.Parse(transObj[i]["price"])/100;
                    long tTime = long.Parse(transObj[i]["transTime"]);
                    DateTimeOffset transTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(transObj[i]["transTime"])/1000);                    
                    double diff = (currTime - transTime).TotalSeconds; // find correct diff
                    if (diff < minTime)
                    {
                        minTime = diff;
                        minTimeIndex = i;
                    }
                    else
                    {
                        if (diff > 60) // Remove expired transaction and put money back into account
                        {
                            var userQuery = db.Execute($"SELECT money FROM user WHERE userid='{command.username}'");
                            var userObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(userQuery);
                            int newBalance = Convert.ToInt32(userObj[0]["money"]) + (int)(amount*100);
                            db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='BUY'");
                            db.ExecuteNonQuery($"UPDATE user SET money={newBalance} WHERE userid='{command.username}'");
                            command.funds = newBalance/100;
                        }
                    }
                }

                if (minTimeIndex >= 0)
                {
                    db.ExecuteNonQuery($"INSERT INTO stocks (userid, stock, price) VALUES ('{command.username}', '{stock}', {amount*100})");
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount*100} AND trasType='BUY'");
                    result = $"Successfully bought {amount} worth of {stock}.";
                } else
                {
                    result = $"No transactions to buy.";
                }
            }
            catch (Exception e)
            {
                result = $"Error occured getting account details.";
                Console.WriteLine(e.ToString());
            }
            // BuyCache.RemoveItems(user);
            LogTransactionEvent(command);
            return result;
        } 
    }
}
