﻿using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
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
                            db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND transTime='{tTime}' AND transType='SELL'");
                            db.ExecuteNonQuery($"UPDATE stocks SET money=money+{amount} WHERE userid='{command.username}' AND stock='{transObj[i]["stock"]}'");
                        }
                    }
                }

                if (minTimeIndex >= 0)
                {
                    stock = transObj[minTimeIndex]["stock"];
                    amount = int.Parse(transObj[minTimeIndex]["price"]);
                    string tTime = transObj[minTimeIndex]["transTime"];
                    db.ExecuteNonQuery($"UPDATE user SET money=money+{amount} WHERE userid='{command.username}'");
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='SELL' AND transTime='{tTime}'");
                    result = $"Successfully sold ${amount/100} worth of {stock}";

                    db.ExecuteNonQuery($"UPDATE stock SET price=price-{amount} WHERE userid='{command.username}' AND stock='{stock}'");
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
