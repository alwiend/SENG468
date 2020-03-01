using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Utilities;
using Constants;
using System.Threading.Tasks;

namespace BuyService
{
    class BuyCancelCommand : BaseService
    {
        public BuyCancelCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            double amount;
            DateTime currTime = DateTime.UtcNow;
            try
            {
                MySQL db = new MySQL();

                var userBalance = await db.ExecuteAsync($"SELECT money FROM user WHERE userid='{command.username}'").ConfigureAwait(false);
                var stockObj = await db.ExecuteAsync($"SELECT stock, price, transTime FROM transactions " +
                    $"WHERE userid='{command.username}' AND transType='BUY'").ConfigureAwait(false);
                int minTimeIndex = -1;
                double minTime = 60.1;
                command.funds = 0;
                for(int i = 0; i < stockObj.Length; i++)
                {
                    amount = double.Parse(stockObj[i]["price"].ToString())/100;
                    long tTime = long.Parse(stockObj[i]["transTime"].ToString());
                    DateTimeOffset transTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(stockObj[i]["transTime"].ToString())/1000);
                    double diff = (currTime - transTime).TotalSeconds;
                    if (diff < minTime)
                    {
                        minTime = diff;
                        minTimeIndex = i;
                    } else
                    {
                        if (diff > 60)
                        {
                            int newB = int.Parse(userBalance[0]["money"].ToString()) + (int)(amount*100);
                            await db.ExecuteNonQueryAsync($"DELETE FROM transactions " +
                                $"WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='BUY'").ConfigureAwait(false);
                            await db.ExecuteNonQueryAsync($"UPDATE user SET money={newB} WHERE userid='{command.username}'").ConfigureAwait(false);
                            command.funds += (decimal)amount;
                        }
                    }
                }
                
                if (minTimeIndex >= 0)
                {
                    amount = int.Parse(stockObj[minTimeIndex]["price"].ToString());
                    string stock = stockObj[minTimeIndex]["stock"].ToString();
                    string tTime = stockObj[minTimeIndex]["transTime"].ToString();
                    var queryRes = await db.ExecuteAsync($"SELECT money FROM user WHERE userid='{command.username}'").ConfigureAwait(false);
                    int newB = int.Parse(queryRes[0]["money"].ToString()) + (int)amount;
                    await db.ExecuteNonQueryAsync($"UPDATE user SET money={newB} WHERE userid='{command.username}'").ConfigureAwait(false);
                    result = $"Successfully canceled most recent buy command.\n" +
                        $"{amount/100} has been added back into your account,";
                    await db.ExecuteNonQueryAsync($"DELETE FROM transactions " +
                        $"WHERE userid='{command.username}' AND stock='{stock}' " +
                        $"AND price={amount} AND transType='BUY' AND transTime='{tTime}'").ConfigureAwait(false);
                    command.funds += (decimal)(amount / 100);
                } else
                {
                    result = await LogErrorEvent(command, "No recent transactions to cancel.").ConfigureAwait(false);
                }
               
            }
            catch (Exception e)
            {
                result = await LogErrorEvent(command, "Error getting account details").ConfigureAwait(false);
                LogDebugEvent(command, e.Message).ConfigureAwait(false);
            }
            LogTransactionEvent(command, "add").ConfigureAwait(false); // Logs all funds returned into the account
            return result;
        }
    }
}
