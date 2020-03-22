using System;
using System.Net.Sockets;
using Database;
using Utilities;
using Constants;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Concurrent;
using System.Linq;

namespace TransactionServer.Services.Sell
{
    class SellCommand : BaseService
    {
        readonly static ConcurrentDictionary<string, ConnectedServices> clientConnections = new ConcurrentDictionary<string, ConnectedServices>();

        public SellCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            clientConnections.Values.ToList().ForEach(conn => conn.Dispose());
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string stockCost = await GetStock(command).ConfigureAwait(false);
            if (stockCost == null)
            {
                return "Quote service unavailable";
            }
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
                result = LogErrorEvent(command, "Error getting account details");
                LogDebugEvent(command, e.Message);
            }
            return result;
        }

        async Task<string> GetStock (UserCommandType command)
        {
            var delay = 500;

            try
            {
                string result = null;
                do
                {
                    ConnectedServices cs = clientConnections.GetOrAdd(command.username, new ConnectedServices());
                    ServiceConnection conn = await cs.GetServiceConnectionAsync(Service.QUOTE_SERVICE).ConfigureAwait(false);
                    if (conn == null)
                    {
                        throw new Exception("Failed to connect to service");
                    }
                    result = await conn.SendAsync(command, true).ConfigureAwait(false);
                    if (result == null)
                        await Task.Delay(delay).ConfigureAwait(false); // Short delay before tring again
                } while (result == null);

                return result;
            }
            catch (Exception ex)
            {
                LogDebugEvent(command, ex.Message);
                return null;
            }
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
