using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Constants;
using Utilities;
using System.Threading.Tasks;

namespace SellService
{
    class SellCommitCommand : BaseService
    {
        public SellCommitCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
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
                var transObj = await db.ExecuteAsync($"SELECT stock, price, transTime FROM transactions WHERE userid='{command.username}' AND transType='SELL'").ConfigureAwait(false);
                double minTime = 60.1;
                int minTimeIndex = -1;
                for (int i = 0; i < transObj.Length; i++)
                {
                    amount = Convert.ToDouble(transObj[i]["price"]);
                    long tTime = Convert.ToInt64(transObj[i]["transTime"]);
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
                            await db.ExecuteNonQueryAsync($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='SELL'").ConfigureAwait(false);
                            await db.ExecuteNonQueryAsync($"UPDATE stocks SET money=money+{amount} WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'").ConfigureAwait(false);
                        }
                    }
                }

                if (minTimeIndex >= 0)
                {
                    stock = transObj[minTimeIndex]["stock"].ToString();
                    amount = Convert.ToInt32(transObj[minTimeIndex]["price"]);
                    string tTime = transObj[minTimeIndex]["transTime"].ToString();
                    await db.ExecuteNonQueryAsync($"UPDATE user SET money=money+{amount} WHERE userid='{command.username}'").ConfigureAwait(false);
                    await db.ExecuteNonQueryAsync($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='SELL' AND transTime='{tTime}'").ConfigureAwait(false);
                    result = $"Successfully sold ${amount/100} worth of {stock}";

                    await db.ExecuteNonQueryAsync($"UPDATE stock SET price=price-{amount} WHERE userid='{command.username}' AND stock='{stock}'").ConfigureAwait(false);
                    command.funds = (decimal)(amount / 100);
                } else
                {
                    result = await LogErrorEvent(command, "No recent transactions to cancel.").ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                result = await LogErrorEvent(command, "Error getting account details").ConfigureAwait(false);
                await LogDebugEvent(command, e.Message).ConfigureAwait(false);
            }
            await LogTransactionEvent(command, "add").ConfigureAwait(false);
            return result;
        }
    }
}
