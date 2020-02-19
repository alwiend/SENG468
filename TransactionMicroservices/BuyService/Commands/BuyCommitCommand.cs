using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
using Constants;
using Utilities;
using System.Threading.Tasks;

namespace BuyService
{
    class BuyCommitCommand : BaseService
    {
        public BuyCommitCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            string stock;
            double amount;
            DateTime currTime = DateTime.UtcNow;
            try
            {
                MySQL db = new MySQL();
                var queryRes = db.Execute($"SELECT stock, price, transTime FROM transactions WHERE userid='{command.username}' AND transType='BUY'");
                var transObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(queryRes);
                double minTime = 60.1;
                int minTimeIndex = -1;
                command.funds = 0;
                for (int i = 0; i < transObj.Length; i++)
                {
                    amount = double.Parse(transObj[i]["price"]) / 100;
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
                            command.funds += (decimal)amount;
                        }
                    }
                }

                if (minTimeIndex >= 0)
                {

                    stock = transObj[minTimeIndex]["stock"];
                    amount = int.Parse(transObj[minTimeIndex]["price"]);
                    string tTime = transObj[minTimeIndex]["transTime"];
                    var hasStock = db.Execute($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{stock}'");
                    var stockObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasStock);
                    string query = $"INSERT INTO stocks (userid, stock, price) VALUES ('{command.username}', '{stock}', {amount})";
                    if (stockObj.Length > 0)
                    {
                        query = $"UPDATE stocks SET price={amount + int.Parse(stockObj[0]["price"])} WHERE userid='{command.username}' AND stock='{stock}'";
                    } 
                    db.ExecuteNonQuery(query);
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='BUY' AND transTime='{tTime}'");
                    result = $"Successfully bought ${amount/100} worth of {stock}.";
                } else
                {
                    result = await LogErrorEvent(command, "No recent transactions to buy.").ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                result = await LogErrorEvent(command, "Error getting account details").ConfigureAwait(false);
                await LogDebugEvent(command, e.Message).ConfigureAwait(false);
            }
            await LogTransactionEvent(command, "remove").ConfigureAwait(false);
            return result;
        } 
    }
}
