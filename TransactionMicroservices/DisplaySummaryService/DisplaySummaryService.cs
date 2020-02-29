using System;
using System.Collections.Generic;
using Base;
using Constants;
using Newtonsoft.Json;
using Utilities;
using Database;
using System.Threading.Tasks;

namespace DisplaySummaryService
{
    class DisplaySummaryService : BaseService
    {
        static async Task Main(string[] args)
        {
            var display_summary_service = new DisplaySummaryService(Service.DISPLAY_SUMMARY_SERVICE, new AuditWriter());
            await display_summary_service.StartService().ConfigureAwait(false);
        }

        public DisplaySummaryService(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                var userObj = await db.ExecuteAsync($"SELECT userid, money FROM user WHERE userid='{command.username}'").ConfigureAwait(false);
                if (userObj.Length > 0)
                {
                    result = $"User {command.username} has ${Convert.ToDouble(userObj[0]["money"]) / 100} in your account.\n";
                    command.funds = Convert.ToDecimal(userObj[0]["money"]) / 100;
                    var stockObj = await db.ExecuteAsync($"SELECT stock, price FROM stocks WHERE userid='{command.username}'").ConfigureAwait(false);
                    if (stockObj.Length > 0)
                    {
                        result += $"{command.username} owns the following stocks:\n";
                        for (int i = 0; i < stockObj.Length; i++)
                        {
                            result += $"{stockObj[i]["stock"]}: {Convert.ToDecimal(stockObj[i]["price"]) / 100m}\n";
                        }
                    }
                    
                } else
                {
                    result = await LogErrorEvent(command, "User does not exist").ConfigureAwait(false);
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
