// Connects to Quote Server and returns quote
using System;
using System.Data;
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
            if(!command.fundsSpecified || command.funds <= 0)
            {
                return "Invalid funds specified.";
            }

            string result;
            try
            {
                MySQL db = new MySQL();

                await db.PerformTransaction(AddMoney, command).ConfigureAwait(false);
                result = $"Successfully added {command.funds/100m} into {command.username}'s account";

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
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "add_user";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pFunds", command.funds);
                cmd.Parameters["@pFunds"].Direction = ParameterDirection.Input;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}