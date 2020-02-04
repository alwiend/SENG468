using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
using Utilities;
using Constants;

namespace BuyService
{
    class BuyCancelCommand : BaseService
    {
        public BuyCancelCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = CancelBuy;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "cancel buy",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        public string CancelBuy(UserCommandType command)
        {
            string result = "";
            double amount = 0.0;
            DateTime currTime = DateTime.UtcNow;
            try
            {
                MySQL db = new MySQL();

                var balance = db.Execute($"SELECT money FROM user WHERE userid='{command.username}'");
                var userBalance = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(balance);
                var stockReq = db.Execute($"SELECT stock, price, transTime FROM transactions WHERE userid='{command.username}' AND transType='BUY'");
                var stockObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(stockReq);
                int minTimeIndex = -1;
                double minTime = 60.1;
                for(int i = 0; i < stockObj.Length; i++)
                {
                    amount = double.Parse(stockObj[i]["price"])/100;
                    long tTime = long.Parse(stockObj[i]["transTime"]);
                    DateTimeOffset transTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(stockObj[i]["transTime"])/1000);
                    double diff = (currTime - transTime).TotalSeconds;
                    if (diff < minTime)
                    {
                        minTime = diff;
                        minTimeIndex = i;
                    } else
                    {
                        if (diff > 60)
                        {
                            int newB = int.Parse(userBalance[0]["money"]) + (int)(amount*100);
                            db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='BUY'");
                            db.ExecuteNonQuery($"UPDATE user SET money={newB} WHERE userid='{command.username}'");
                            command.funds = newB/100;
                        }
                    }
                }
                
                if (minTimeIndex >= 0)
                {
                    amount = int.Parse(stockObj[minTimeIndex]["price"]);
                    string stock = stockObj[minTimeIndex]["stock"];
                    int newB = Convert.ToInt32(userBalance[0]["money"]) + (int)(amount);
                    db.ExecuteNonQuery($"UPDATE user SET money={newB} WHERE userid='{command.username}'");
                    result = $"Successfully canceled most recent buy command.\n" +
                        $"Your new balance is {newB/100}";
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='BUY'");
                } else
                {
                    result = "No transactions to cancel.";
                }
               
            }
            catch (Exception e)
            {
                result = $"{e.ToString()} Error occured changing acccount balance";
                Console.WriteLine(e.ToString());
            }
            // BuyCache.RemoveItems(user);
            LogTransactionEvent(command);
            return result;
        }
    }
}
