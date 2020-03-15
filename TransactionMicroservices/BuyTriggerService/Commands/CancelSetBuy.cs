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
    public class CancelSetBuy : BaseService
    {
        public CancelSetBuy(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result = "";
            try
            {
                var trigger = BuyTriggerTimer.RemoveUserTrigger(command.username, command.stockSymbol);
                if (trigger != null)
                {
                    command.fundsSpecified = true;
                    command.funds = trigger.Amount;
                    // Check if trigger exists
                    MySQL db = new MySQL();
                    result = await db.PerformTransaction(CancelBuy, command).ConfigureAwait(false);
                } else
                {
                    return await LogErrorEvent(command, $"No trigger set for {command.stockSymbol}");
                }
            }
            catch (Exception ex)
            {
                await LogDebugEvent(command, ex.Message).ConfigureAwait(false);
                return await LogErrorEvent(command, "Error processing command").ConfigureAwait(false);
            }
            return result;
        }

        async Task<string> CancelBuy(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "return_user_money";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pMoney", command.funds);
                cmd.Parameters["@pMoney"].Direction = ParameterDirection.Input;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                await LogTransactionEvent(command, "add").ConfigureAwait(false);
                return $"Successfully removed trigger to buy stock {command.stockSymbol}";
            }
        }
    }
}
