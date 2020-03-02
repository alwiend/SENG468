using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
using Constants;
using Utilities;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace BuyService
{
    class BuyCommitCommand : BaseService
    {
        public BuyCommitCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            // CONVERT Transation time to big int in schema
            // Convert this to stored procedure

            string result;
            string stock;
            double amount;
            DateTime currTime = DateTime.UtcNow;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(BuyCommit, command).ConfigureAwait(false);

                var transObj = await db.ExecuteAsync($"SELECT stock, price, transTime FROM transactions " +
                    $"WHERE userid='{command.username}' AND transType='BUY'").ConfigureAwait(false);
                
                double minTime = 60.1;
                int minTimeIndex = -1;
                command.funds = 0;
                for (int i = 0; i < transObj.Length; i++)
                {
                    amount = Convert.ToDouble(transObj[i]["price"]) / 100;
                    long tTime = Convert.ToInt64(transObj[i]["transTime"]);
                    DateTimeOffset transTime = DateTimeOffset.FromUnixTimeSeconds(tTime/1000);                    
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
                            var userObj = await db.ExecuteAsync($"SELECT money FROM user WHERE userid='{command.username}'").ConfigureAwait(false);
                            int newBalance = Convert.ToInt32(userObj[0]["money"]) + (int)(amount*100);
                            await db.ExecuteNonQueryAsync($"DELETE FROM transactions WHERE userid='{command.username}' " +
                                $"AND transTime='{tTime}' AND transType='BUY'").ConfigureAwait(false);
                            await db.ExecuteNonQueryAsync($"UPDATE user SET money={newBalance} WHERE userid='{command.username}'").ConfigureAwait(false);
                            command.funds += (decimal)amount;
                        }
                    }
                }

                if (minTimeIndex >= 0)
                {
                    stock = transObj[minTimeIndex]["stock"].ToString();
                    amount = Convert.ToInt32(transObj[minTimeIndex]["price"]);
                    string tTime = transObj[minTimeIndex]["transTime"].ToString();
                    var stockObj = await db.ExecuteAsync($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{stock}'").ConfigureAwait(false);
                    string query = $"INSERT INTO stocks (userid, stock, price) VALUES ('{command.username}', '{stock}', {amount})";
                    if (stockObj.Length > 0)
                    {
                        query = $"UPDATE stocks SET price={amount + Convert.ToInt32(stockObj[0]["price"])} WHERE userid='{command.username}' AND stock='{stock}'";
                    } 
                    await db.ExecuteNonQueryAsync(query).ConfigureAwait(false);
                    await db.ExecuteNonQueryAsync($"DELETE FROM transactions WHERE userid='{command.username}' " +
                        $"AND stock='{stock}' AND price={amount} AND transType='BUY' AND transTime='{tTime}'").ConfigureAwait(false);
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


        async Task<string> BuyCommit(MySqlConnection cnn, UserCommandType command)
        {
            return "";
        }
    }
}
