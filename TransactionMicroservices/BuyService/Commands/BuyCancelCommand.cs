using System;
using Database;
using Utilities;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace TransactionServer.Services.Buy
{
    class BuyCancelCommand : BaseService
    {
        public BuyCancelCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(BuyCancel, command).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                result = LogErrorEvent(command, "Error getting account details");
                LogDebugEvent(command, e.Message);
            }
            return result;
        }


        async Task<string> BuyCancel(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "buy_cancel_stock";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pServerTime", Unix.TimeStamp);
                cmd.Parameters["@pServerTime"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@message", MySqlDbType.Text));
                cmd.Parameters["@message"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@pStock", MySqlDbType.VarChar, 3));
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@pStockAmount", MySqlDbType.Int32));
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (!Convert.ToBoolean(cmd.Parameters["@success"].Value))
                {
                    return LogErrorEvent(command, Convert.ToString(cmd.Parameters["@message"].Value));
                }
                return $"Successfully cancelled {Convert.ToDecimal(cmd.Parameters["@pStockAmount"].Value) / 100m} worth of {cmd.Parameters["@pStock"].Value}.";
            }
        }
    }
}
