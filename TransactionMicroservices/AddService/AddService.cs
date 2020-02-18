// Connects to Quote Server and returns quote
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Base;
using Constants;
using Database;
using Newtonsoft.Json;
using Utilities;

namespace AddService
{

    public class AddService : BaseService
    {
        public static async Task Main(string[] args)
        {
            var add_service = new AddService(Service.ADD_SERVICE, new AuditWriter());
            await add_service.StartService().ConfigureAwait(false);
        }

        public AddService (ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        // ExecuteClient() Method 
        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                long funds = (long)(command.funds * 100);
                string query = $"INSERT INTO user (userid, money) VALUES ('{command.username}',{funds})";
                if (userObject.Length > 0)
                {
                    funds += long.Parse(userObject[0]["money"]);
                    query = $"UPDATE user SET money={funds} WHERE userid='{command.username}'";
                }

                db.ExecuteNonQuery(query);
                result = $"Successfully added {command.funds} into {command.username}'s account";

                await LogTransactionEvent(command, "add").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await LogDebugEvent(command, e.Message).ConfigureAwait(false);
                result = await LogErrorEvent(command, "Error occurred adding money.").ConfigureAwait(false);
            }
            return result;
        }
    }
}