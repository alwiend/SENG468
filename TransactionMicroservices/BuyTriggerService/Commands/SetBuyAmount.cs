using Base;
using Constants;
using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace BuyTriggerService
{
    public class SetBuyAmount : BaseService
    {
        public SetBuyAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
            DataReceived = SetAmount;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "remove",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        string LogUserErrorEvent(UserCommandType command)
        {
            ErrorEventType error = new ErrorEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                errorMessage = "Trigger/User does not exist OR insufficeint funds"
            };
            Auditor.WriteRecord(error);
            return error.errorMessage;
        }

        string LogDBErrorEvent(UserCommandType command)
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
                errorMessage = "Error processing command"
            };
            Auditor.WriteRecord(error);
            return error.errorMessage;
        }

        void LogDebugEvent(UserCommandType command, Exception ex)
        {
            DebugType bug = new DebugType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                debugMessage = ex.ToString()
            };
            Auditor.WriteRecord(bug);
        }

        private string SetAmount(UserCommandType command)
        {
            string result = "";
            try
            {
                // Check user account for cash, notify if insufficient funds
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length <= 0)
                {
                    return LogUserErrorEvent(command);
                }

                decimal balance = decimal.Parse(userObject[0]["money"]) / 100.0m; // normalize

                if (balance < command.funds)
                {
                    return LogUserErrorEvent(command);
                }

                // Check if there is already a trigger
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length > 0)
                {
                    return LogUserErrorEvent(command);
                }

                // Set aside required cash
                balance -= command.funds;
                db.ExecuteNonQuery($"UPDATE user SET money={(long)(balance*100)} WHERE userid='{command.username}'");

                // Update triggers with details
                db.ExecuteNonQuery($"INSERT INTO triggers (userid,stock,amount,triggerType) " +
                    $"VALUES ('{command.username}','{command.stockSymbol}',{(long)(command.funds*100)},'BUY')");

                result = "Trigger Created";
            }
            catch (Exception ex)
            {
                LogDebugEvent(command, ex);
                return LogDBErrorEvent(command);
            }
            LogTransactionEvent(command);
            return result;
        }
    }
}
