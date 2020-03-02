// Connects to Quote Server and returns quote
using System;
using System.Threading.Tasks;
using Base;
using Constants;
using Database;
using MySql.Data.MySqlClient;
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

                await db.PerformTransaction(AddMoney, command).ConfigureAwait(false);
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

        private async Task AddMoney(MySqlConnection cnn, UserCommandType command)
        {
            var sql = "INSERT INTO user (userid, money) VALUES (@userid,@funds) " +
                "ON DUPLICATE KEY UPDATE money = money + @funds;";
            
            using (MySqlCommand cmd = new MySqlCommand(sql, cnn))
            {
                cmd.Parameters.AddWithValue("@userid", command.username);
                cmd.Parameters.AddWithValue("@funds", command.funds);
                await cmd.PrepareAsync().ConfigureAwait(false);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}