using System;
using Database;
using Utilities;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace TransactionServer.Services.Sell
{
    class SellCommitCommand : BaseService
    {
        public SellCommitCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }


        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(SellCommit, command).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                result = LogErrorEvent(command, "Error getting account details");
                LogDebugEvent(command, e.Message);
            }
            LogTransactionEvent(command, "add");
            return result;
        }

        async Task<string> SellCommit(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "sell_commit";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@pServerTime", MySqlDbType.Text));
                cmd.Parameters["@pServerTime"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@pStock", MySqlDbType.Text));
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@pStockAmount", MySqlDbType.Int32));
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@message", MySqlDbType.Text));
                cmd.Parameters["@message"].Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (!Convert.ToBoolean(cmd.Parameters["@success"].Value))
                {
                    return Convert.ToString(cmd.Parameters["@message"].Value);
                }
                command.funds = Convert.ToDecimal(cmd.Parameters["@pStockAmount"].Value);
                command.stockSymbol = Convert.ToString(cmd.Parameters["@pStock"].Value);
                return $"Successfully sold ${command.funds / 100} worth of {command.stockSymbol}";
            }
        }
    }
}
