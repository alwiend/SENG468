using System;
using Base;
using Database;
using Utilities;
using Constants;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace BuyService
{
    class BuyCommand : BaseService
    {
        public BuyCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string stockCost = await GetStock(command).ConfigureAwait(false);
            decimal cost = Convert.ToDecimal(stockCost);

            int numStock = (int)Math.Floor(command.funds / cost); // most whole number stock that can buy
            if (numStock == 0)
            {
                return $"No stock available for ${command.funds}";
            }

            command.funds = cost * numStock;

            string result;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(BuyStock, command).ConfigureAwait(false);
                
                if (result == null)
                {
                    result = $"{numStock} stock is available for purchase at {stockCost} per share totalling {String.Format("{0:0.00}", command.funds)}.";
                }
            }
            catch (Exception e)
            {
                result = await LogErrorEvent(command, "Error getting account details").ConfigureAwait(false);
                await LogDebugEvent(command, e.Message).ConfigureAwait(false);
            }
            return result;
        }

        async Task<string> GetStock(UserCommandType command)
        {
            ServiceConnection conn = new ServiceConnection(Server.QUOTE_SERVER);
            string quote = await conn.Send($"{command.stockSymbol},{command.username}", true).ConfigureAwait(false);
            return await LogQuoteServerEvent(command, quote).ConfigureAwait(false);
        }

        async Task<string> BuyStock(MySqlConnection cnn, UserCommandType command)
        {            
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "buy_stock";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", command.stockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStockAmount", (int)(command.funds*100));
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pServerTime", Unix.TimeStamp);
                cmd.Parameters["@pServerTime"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;
                cmd.Parameters.Add(new MySqlParameter("@message", MySqlDbType.Text));
                cmd.Parameters["@message"].Direction = ParameterDirection.Output;
                
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (!Convert.ToBoolean(cmd.Parameters["@success"].Value))
                {
                    return await LogErrorEvent(command, Convert.ToString(cmd.Parameters["@message"].Value)).ConfigureAwait(false);
                }
                await LogTransactionEvent(command, "remove").ConfigureAwait(false);
                return null;
            }
        }
    }
}
