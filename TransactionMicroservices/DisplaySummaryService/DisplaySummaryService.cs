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
