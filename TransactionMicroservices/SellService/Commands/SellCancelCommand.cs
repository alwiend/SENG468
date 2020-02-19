using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Utilities;
using Constants;
using System.Threading.Tasks;

namespace SellService
{
    class SellCancelCommand : BaseService
    {
        public SellCancelCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
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
                var transObj = await db.ExecuteAsync($"SELECT stock, price, transTime FROM transactions " +
                    $"WHERE userid='{command.username}' AND transType='SELL'").ConfigureAwait(false);
                int minTimeIndex = -1;
                double minTime = 60.1;
                command.funds = 0;
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
                    } else
                    {
                        if (timeDiff > 60)
                        {
                            await db.ExecuteNonQueryAsync($"UPDATE stocks SET price=price+{amount} WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'").ConfigureAwait(false);
                            await db.ExecuteNonQueryAsync($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='SELL' AND stock='{transObj[0]["stock"]}'").ConfigureAwait(false);
                        }
                    }
                }

                if (minTimeIndex >=0)
                {
                    amount = Convert.ToInt32(transObj[minTimeIndex]["price"]);
                    string stock = transObj[minTimeIndex]["stock"].ToString();
                    string tTime = transObj[minTimeIndex]["transTime"].ToString();
                    await db.ExecuteNonQueryAsync($"UPDATE stocks SET price=price+{amount} WHERE userid='{command.username}' AND stock='{stock}'").ConfigureAwait(false);
                    result = $"Successfully canceled most recent sell command. \n" +
                        $"You have ${amount / 100} of {stock} back into your account.";
                    await db.ExecuteNonQueryAsync($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='SELL' AND transTime='{tTime}'").ConfigureAwait(false);
                    command.funds = (decimal)(amount / 100);
                    command.stockSymbol = stock;
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
            return result;
        }
    }
}
