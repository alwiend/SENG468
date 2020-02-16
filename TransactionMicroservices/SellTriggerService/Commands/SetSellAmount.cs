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
    public class SetSellAmount : BaseService
    {
        public SetSellAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
            DataReceived = SetAmount;
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
                errorMessage = "Tigger already exists OR Insufficient stock"
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
                // Check user account for stock, notify if insufficient
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT price FROM stocks" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length == 0)
                {
                    return LogUserErrorEvent(command);
                }

                int balance = int.Parse(userObject[0]["price"]);
                // normalize
                int quantity = (int)(command.funds * 100);

                if (balance < quantity)
                {
                    return LogUserErrorEvent(command);
                }

                // Check if there is already a trigger
                var hasTrigger = db.Execute($"SELECT amount FROM triggers " +
                    $"WHERE userid='{command.username}' AND stock='{command.stockSymbol}' AND triggerType='SELL'");
                var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
                if (triggerObject.Length > 0)
                {
                    return LogUserErrorEvent(command);
                }

                // Set aside required stock
                balance -= quantity;
                db.ExecuteNonQuery($"UPDATE stocks SET price={balance}" +
                    $" WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");

                // Update triggers with details
                db.ExecuteNonQuery($"INSERT INTO triggers (userid,stock,amount,triggerType) " +
                    $"VALUES ('{command.username}','{command.stockSymbol}',{quantity},'SELL')");

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
