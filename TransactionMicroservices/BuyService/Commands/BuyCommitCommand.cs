using System;
using Database;
using Utilities;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace TransactionServer.Services.Buy
{
    class BuyCommitCommand : BaseService
    {
        public BuyCommitCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(BuyCommit, command).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                result = LogErrorEvent(command, "Error getting account details");
                LogDebugEvent(command, e.Message);
            }
            return result;
        }

        async Task<string> BuyCommit(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "buy_commit";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pServerTime", Unix.TimeStamp);
                cmd.Parameters["@pServerTime"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@message", MySqlDbType.Text));
                cmd.Parameters["@message"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@stockBuy", MySqlDbType.VarChar, 3));
                cmd.Parameters["@stockBuy"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@stockAmount", MySqlDbType.Int32));
                cmd.Parameters["@stockAmount"].Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (!Convert.ToBoolean(cmd.Parameters["@success"].Value))
                {
                    return LogErrorEvent(command, Convert.ToString(cmd.Parameters["@message"].Value));
                }
                
                return $"Successfully bought ${String.Format("{0:0.00}", Convert.ToDecimal(cmd.Parameters["@stockAmount"].Value) / 100m)} worth of {cmd.Parameters["@stockBuy"].Value}.";
            }
        }
    }
}
