using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
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
