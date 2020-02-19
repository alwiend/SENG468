using System;
using System.Collections.Generic;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;
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

        async Task LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "add",
                username = command.username,
                funds = command.funds
            };
            await Auditor.WriteRecord(transaction).ConfigureAwait(false);
        }

        async Task<string> LogUserErrorEvent(UserCommandType command)
        {
            ErrorEventType error = new ErrorEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                errorMessage = "No recent transactions to cancel."
            };
            await Auditor.WriteRecord(error).ConfigureAwait(false);
            return error.errorMessage;
        }

        async Task<string> LogDBErrorEvent(UserCommandType command)
        {
            ErrorEventType error = new ErrorEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                stockSymbol = command.stockSymbol,
                funds = command.funds,
                errorMessage = "Error getting account details"
            };
            await Auditor.WriteRecord(error).ConfigureAwait(false);
            return error.errorMessage;
        }

        async Task LogDebugEvent(UserCommandType command, Exception e)
        {
            DebugType bug = new DebugType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                debugMessage = e.ToString()
            };
            await Auditor.WriteRecord(bug).ConfigureAwait(false);
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            double amount;
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
                command.funds = 0;
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
                            command.funds += (decimal)amount;
                        }
                    }
                }
                
                if (minTimeIndex >= 0)
                {
                    amount = int.Parse(stockObj[minTimeIndex]["price"]);
                    string stock = stockObj[minTimeIndex]["stock"];
                    string tTime = stockObj[minTimeIndex]["transTime"];
                    var userQuery = db.Execute($"SELECT money FROM user WHERE userid='{command.username}'");
                    var queryRes = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(userQuery);
                    int newB = int.Parse(queryRes[0]["money"]) + (int)amount;
                    db.ExecuteNonQuery($"UPDATE user SET money={newB} WHERE userid='{command.username}'");
                    result = $"Successfully canceled most recent buy command.\n" +
                        $"{amount/100} has been added back into your account,";
                    db.ExecuteNonQuery($"DELETE FROM transactions WHERE userid='{command.username}' AND stock='{stock}' AND price={amount} AND transType='BUY' AND transTime='{tTime}'");
                    command.funds += (decimal)(amount / 100);
                } else
                {
                    result = await LogUserErrorEvent(command).ConfigureAwait(false);
                }
               
            }
            catch (Exception e)
            {
                result = await LogDBErrorEvent(command).ConfigureAwait(false);
                await LogDebugEvent(command, e).ConfigureAwait(false);
            }
            await LogTransactionEvent(command).ConfigureAwait(false); // Logs all funds returned into the account
            return result;
        }
    }
}
