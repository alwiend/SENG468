﻿using Database;
using System;
using System.Threading.Tasks;
using Utilities;
using MySql.Data.MySqlClient;
using System.Data;

namespace TransactionServer.Services.SellTrigger
{
    public class CancelSetSell : BaseService
    {
        public CancelSetSell(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result = "";
            try
            {
                var trigger = SellTriggerTimer.RemoveUserTrigger(command.username, command.stockSymbol);
                if (trigger != null)
                {
                    command.fundsSpecified = true;
                    command.funds = trigger.Amount;
                    // Check if trigger exists
                    MySQL db = new MySQL();
                    result = await db.PerformTransaction(CancelSell, command).ConfigureAwait(false);
                }
                else
                {
                    return LogErrorEvent(command, $"No trigger set for {command.stockSymbol}");
                }
            }
            catch (Exception ex)
            {
                LogDebugEvent(command, ex.Message);
                return LogErrorEvent(command, "Error processing command");
            }
            return result;
        }

        async Task<string> CancelSell(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "return_user_stock";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", command.stockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStockAmount", command.funds);
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Input;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                return $"Successfully removed trigger to sell stock {command.stockSymbol}";
            }
        }
    }
}
