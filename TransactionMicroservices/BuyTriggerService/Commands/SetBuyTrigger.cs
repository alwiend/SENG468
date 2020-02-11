using System;
using System.Collections.Generic;
using System.Text;

using Constants;
using Base;
using Utilities;
using Database;
using Newtonsoft.Json;

namespace BuyTriggerService
{
    public class SetBuyTrigger : BaseService
    {
        public SetBuyTrigger(ServiceConstant sc, AuditWriter aw) : base(sc,aw)
        {
            DataReceived = SetTrigger;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            SystemEventType transaction = new SystemEventType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                username = command.username,
                funds = command.funds,
                stockSymbol = command.stockSymbol
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
                errorMessage = "Trigger does not exist OR is already set."
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
                errorMessage = "Error processing command."
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

        private string SetTrigger(UserCommandType command)
        {
            string result;
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL();

                var hasTrigger = db.Execute($"SELECT amount,triggerAmount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length == 0)
                {
                    return LogUserErrorEvent(command);
                }

                if (Convert.ToInt32(triggerObject[0]["triggerAmount"])/100m == command.funds)
                {
                    return LogUserErrorEvent(command);
                }

                // Remove trigger
                db.ExecuteNonQuery($"UPDATE triggers " +
                    $"SET triggerAmount={command.funds*100}" +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='BUY'");

                result = "Trigger amount set";
                decimal amount = Convert.ToDecimal(triggerObject[0]["amount"]) / 100m;
                var timer = new BuyTriggerTimer(command.username, command.stockSymbol, amount, command.funds);
                timer.Start();
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
