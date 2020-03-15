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

namespace BuyTriggerService
{
    public class SetBuyAmount : BaseService
    {
        public SetBuyAmount(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
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
                var msg = await CheckMoney(cmd, command).ConfigureAwait(false);
                if (msg != null)
                {
                    return await LogErrorEvent(command, msg).ConfigureAwait(false);
                }
                cmd.Parameters.Clear();
                await SetUserMoney(cmd, command).ConfigureAwait(false);
            }
            await LogTransactionEvent(command, "remove").ConfigureAwait(false);
            return $"Buy amount set successfully for stock {command.stockSymbol}";
        }

        async Task<string> CheckMoney(MySqlCommand cmd, UserCommandType command)
        {
            cmd.CommandText = "get_user_money";

            cmd.Parameters.AddWithValue("@pUserId", command.username);
            cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
            cmd.Parameters.Add("@pMoney", DbType.Int32);
            cmd.Parameters["@pMoney"].Direction = ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            if (cmd.Parameters["@pMoney"].Value == DBNull.Value)
            {
                return await LogErrorEvent(command, "User does not exist").ConfigureAwait(false); ;
            }
            if (Convert.ToInt32(cmd.Parameters["@pMoney"].Value) < command.funds)
            {
                return await LogErrorEvent(command, "Insufficient user funds").ConfigureAwait(false); ;
            }

            command.fundsSpecified = false;
            return await BuyTriggerTimer.StartOrUpdateTimer(command).ConfigureAwait(false); ;
        }

        async Task<string> SetUserMoney(MySqlCommand cmd, UserCommandType command)
        {
            cmd.CommandText = "hold_user_money";

            cmd.Parameters.AddWithValue("@pUserId", command.username);
            cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
            cmd.Parameters.AddWithValue("@pMoney", (int)command.funds);
            cmd.Parameters["@pMoney"].Direction = ParameterDirection.Input;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            return null;
        }
    }
}
