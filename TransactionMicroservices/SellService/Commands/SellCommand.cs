using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Base;
using Database;
using Utilities;
using Constants;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace SellService
{
    class SellCommand : BaseService
    {
        public SellCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }


        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string stockCost = await GetStock(command).ConfigureAwait(false);
            decimal cost = Convert.ToDecimal(stockCost);

            int numStock = (int)Math.Floor(command.funds / cost);
            if (numStock == 0)
            {
                return $"Stock not able to sell for ${command.funds}";
            }

            command.funds = cost * numStock;

            string result;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(SellStock, command).ConfigureAwait(false);
                if (result == null)
                {
                    result = $"{numStock} stock is available for sale at {cost/100m} per share totalling {String.Format("{0:0.00}", command.funds/100m)}";
                }
            }
            catch (Exception e)
            {
                result = await LogErrorEvent(command, "Error getting account details").ConfigureAwait(false);
                await LogDebugEvent(command, e.Message).ConfigureAwait(false);
            }
            return result;
        }

        async Task<string> GetStock (UserCommandType command)
        {
            ServiceConnection conn = new ServiceConnection(Service.QUOTE_SERVICE);
            return await conn.Send(command, true).ConfigureAwait(false);
        }

        async Task<string> SellStock(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "sell_stock";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", command.stockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStockAmount", (int)(command.funds));
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pServerTime", Unix.TimeStamp.ToString());
                cmd.Parameters["@pServerTime"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@message", MySqlDbType.Text));
                cmd.Parameters["@message"].Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (!Convert.ToBoolean(cmd.Parameters["@success"].Value))
                {
                    return Convert.ToString(cmd.Parameters["@message"].Value);
                }
                return null;
            }
        }
    }
}
