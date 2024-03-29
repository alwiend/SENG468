﻿using System;
using Database;
using Utilities;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace TransactionServer.Services.Sell
{
    class SellCancelCommand : BaseService
    {
        public SellCancelCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(SellCancel, command).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                result = LogErrorEvent(command, "Error getting account details");
                LogDebugEvent(command, e.Message);
            }
            return result;
        }

        async Task<string> SellCancel(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "sell_cancel_stock";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@pStock", MySqlDbType.Text));
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@pStockAmount", MySqlDbType.Int32));
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@pServerTime", MySqlDbType.Text));
                cmd.Parameters["@pServerTime"].Direction = ParameterDirection.Output;
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
                return $"Successfully canceled most recent sell transaction of {command.stockSymbol} worth ${command.funds / 100}";
            }
        }
    }
}
