using Base;
using Constants;
using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SellTriggerService
{
    public class CancelSetSell : BaseService
    {
        public CancelSetSell(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
            DataReceived = CancelTrigger;
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
                errorMessage = "Trigger does not exist"
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

        private string CancelTrigger(UserCommandType command)
        {
            string result;
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL(); 
                
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length == 0)
                {
                    return LogUserErrorEvent(command);
                }

                // Return stock to user account
                db.ExecuteNonQuery($"UPDATE stocks SET price=price+{triggerObject[0]["amount"]}" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                command.funds = decimal.Parse(triggerObject[0]["amount"])/100;
                // Remove trigger
                db.ExecuteNonQuery($"DELETE FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");

                result = "Trigger removed";
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
