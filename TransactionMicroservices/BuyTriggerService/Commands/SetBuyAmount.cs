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
                cmd.CommandText = "set_buy_amount";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", command.stockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pBuyAmount", (int)(command.funds * 100));
                cmd.Parameters["@pBuyAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@message", MySqlDbType.Text));
                cmd.Parameters["@message"].Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (!Convert.ToBoolean(cmd.Parameters["@success"].Value))
                {
                    return Convert.ToString(cmd.Parameters["@message"].Value);
                }
                await LogTransactionEvent(command, "remove").ConfigureAwait(false);
                return $"Trigger created successfully for stock {command.stockSymbol}";
            }
        }
    }
}
