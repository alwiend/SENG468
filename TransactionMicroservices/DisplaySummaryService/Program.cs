using System;
using System.Collections.Generic;
using Base;
using Constants;
using Newtonsoft.Json;
using Utilities;
using Database;

namespace DisplaySummaryService
{
    class DisplaySummaryService : BaseService
    {
        static void Main(string[] args)
        {
            var display_summary_service = new DisplaySummaryService(Service.DISPLAY_SUMMARY_SERVICE, new AuditWriter());
            display_summary_service.StartService();
        }

        public DisplaySummaryService(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = DisplayAccount;
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
                stockSymbol = command.stockSymbol,
                funds = command.funds,
                errorMessage = "User does not exist"
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
                errorMessage = "Error getting account details"
            };
            Auditor.WriteRecord(error);
            return error.errorMessage;
        }

        void LogDebugEvent(UserCommandType command, Exception e)
        {
            DebugType bug = new DebugType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                debugMessage = e.ToString()
            };
            Auditor.WriteRecord(bug);
        }

        object DisplayAccount(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObj.Length > 0)
                {
                    result = $"User {command.username} has ${double.Parse(userObj[0]["money"]) / 100} in your account.\n";
                    command.funds = decimal.Parse(userObj[0]["money"]) / 100;
                    var hasStock = db.Execute($"SELECT stock, price FROM stocks WHERE userid='{command.username}'");
                    var stockObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasStock);
                    if (stockObj.Length > 0)
                    {
                        result += $"{command.username} owns the following stocks:\n";
                        for (int i = 0; i < stockObj.Length; i++)
                        {
                            result += $"{stockObj[i]["stock"]}: {double.Parse(stockObj[i]["price"]) / 100}\n";
                        }
                    }
                    
                } else
                {
                    result = LogUserErrorEvent(command);
                }
            }
            catch (Exception e)
            {
                result = LogDBErrorEvent(command);
                LogDebugEvent(command, e);
            }
            LogTransactionEvent(command);
            return result;
        }
    }
}
