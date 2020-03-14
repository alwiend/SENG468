using System;
using System.Collections.Generic;
using System.Text;

using Constants;
using Base;
using Utilities;
using Database;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace SellTriggerService
{
    public class SetSellTrigger : BaseService
    {
        public SetSellTrigger(ServiceConstant sc, AuditWriter aw) : base(sc,aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                // Check if trigger exists
                MySQL db = new MySQL();
                result = await db.PerformTransaction(SetTrigger, command).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await LogDebugEvent(command, ex.Message).ConfigureAwait(false);
                return await LogErrorEvent(command, "Error processing command").ConfigureAwait(false);
            }
            return result;
        }

        async Task<string> SetTrigger(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "set_trigger_amount";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", command.stockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pTriggerAmount", (int)(command.funds * 100));
                cmd.Parameters["@pTriggerAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pTriggerType", "SELL");
                cmd.Parameters["@pTriggerType"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@stockAmount", MySqlDbType.Int32));
                cmd.Parameters["@stockAmount"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@message", MySqlDbType.Text));
                cmd.Parameters["@message"].Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (!Convert.ToBoolean(cmd.Parameters["@success"].Value))
                {
                    return Convert.ToString(cmd.Parameters["@message"].Value);
                }
                decimal amount = Convert.ToDecimal(cmd.Parameters["@stockAmount"].Value) / 100m;
                var timer = new SellTriggerTimer(command.username, command.stockSymbol, amount, command.funds);
                timer.Start();
                return $"Trigger amount ${command.funds} set for stock {command.stockSymbol}";
            }
        }
    }
}
