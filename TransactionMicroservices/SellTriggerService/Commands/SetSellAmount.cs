using Base;
using Constants;
using Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using MySql.Data.MySqlClient;
using System.Data;

namespace SellTriggerService
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
                await LogDebugEvent(command, ex.Message).ConfigureAwait(false);
                return await LogErrorEvent(command, "Error processing command").ConfigureAwait(false);
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
                    return await LogErrorEvent(command, msg).ConfigureAwait(false);
                }
                cmd.Parameters.Clear();
                await HoldUserStock(cmd, command).ConfigureAwait(false);
            }
            await LogTransactionEvent(command, "remove").ConfigureAwait(false);
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
                return await LogErrorEvent(command, "User does not exist or does not have this stock").ConfigureAwait(false); ;
            }
            if (Convert.ToInt32(cmd.Parameters["@pStockAmount"].Value) < command.funds)
            {
                return await LogErrorEvent(command, "Insufficient user stocks").ConfigureAwait(false); ;
            }

            command.fundsSpecified = false;
            return await SellTriggerTimer.StartOrUpdateTimer(command).ConfigureAwait(false); ;
        }

        async Task<string> HoldUserStock(MySqlCommand cmd, UserCommandType command)
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
