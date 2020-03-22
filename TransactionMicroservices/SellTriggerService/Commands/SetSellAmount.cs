using Database;
using System;
using System.Threading.Tasks;
using Utilities;
using MySql.Data.MySqlClient;
using System.Data;

namespace TransactionServer.Services.SellTrigger
{
    public class SetSellAmount : BaseService
    {
        public SetSellAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result = "";
            try
            {
                // Check user account for cash, notify if insufficient funds
                MySQL db = new MySQL();
                result = await db.PerformTransaction(SetAmount, command).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogDebugEvent(command, ex.Message);
                return LogErrorEvent(command, "Error processing command");
            }
            return result;
        }

        async Task<string> SetAmount(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                var msg = await CheckUserStock(cmd, command).ConfigureAwait(false);
                if (msg != null)
                {
                    return LogErrorEvent(command, msg);
                }
                cmd.Parameters.Clear();
                await HoldUserStock(cmd, command).ConfigureAwait(false);
            }
            LogTransactionEvent(command, "remove");
            return $"Sell amount set successfully for stock {command.stockSymbol}";
        }

        async Task<string> CheckUserStock(MySqlCommand cmd, UserCommandType command)
        {
            cmd.CommandText = "get_user_stock";

            cmd.Parameters.AddWithValue("@pUserId", command.username);
            cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
            cmd.Parameters.AddWithValue("@pStock", command.stockSymbol);
            cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
            cmd.Parameters.Add("@pStockAmount", DbType.Int32);
            cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            if (cmd.Parameters["@pStockAmount"].Value == DBNull.Value)
            {
                return LogErrorEvent(command, "User does not exist or does not have this stock");
            }
            if (Convert.ToInt32(cmd.Parameters["@pStockAmount"].Value) < command.funds)
            {
                return LogErrorEvent(command, "Insufficient user stocks");
            }

            command.fundsSpecified = false;
            return await SellTriggerTimer.StartOrUpdateTimer(command).ConfigureAwait(false); ;
        }

        static async Task<string> HoldUserStock(MySqlCommand cmd, UserCommandType command)
        {
            cmd.CommandText = "hold_user_stock";

            cmd.Parameters.AddWithValue("@pUserId", command.username);
            cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
            cmd.Parameters.AddWithValue("@pStock", command.stockSymbol);
            cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
            cmd.Parameters.AddWithValue("@pStockAmount", (int)command.funds);
            cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Input;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            return null;
        }
    }
}
