using Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using MySql.Data.MySqlClient;
using System.Data;

namespace BuyTriggerService
{
    public class BuyTriggerTimer
    {
        public string User { get; }
        public string StockSymbol { get; }
        public string Type { get; }
        public decimal Amount { get; }
        public decimal Trigger { get; }

        public BuyTriggerTimer(string user, string ss, decimal amount, decimal trigger)
        {
            User = user;
            StockSymbol = ss;
            Type = "BUY";
            Amount = amount;
            Trigger = trigger;
        }

        public async Task Start()
        {
            await Run().ConfigureAwait(false);
        }

        async Task Run()
        {
            MySQL db = new MySQL();
            while (true)
            {
                // Check if trigger still exists
                if (!await db.PerformTransaction(IfTriggerExists).ConfigureAwait(false))
                {
                    return;
                }

                ServiceConnection conn = new ServiceConnection(Constants.Server.QUOTE_SERVER);
                string response = await conn.Send($"{StockSymbol},{User}", true).ConfigureAwait(false);
                string[] args = response.Split(",");

                decimal cost = Convert.ToDecimal(args[0]);
                if (cost > Trigger)
                {
                    // Run trigger again
                    int interval = (int)(60000 - (Unix.TimeStamp - Convert.ToInt64(args[3])));
                    await Task.Delay(interval).ConfigureAwait(false);
                } else
                {
                    await db.PerformTransaction(BuyStock, cost).ConfigureAwait(false);
                    return;
                }
            }
        }

        async Task<bool> IfTriggerExists(MySqlConnection cnn)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "check_trigger_exists";

                cmd.Parameters.AddWithValue("@pUserId", User);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", StockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStockAmount", (int)(Amount * 100));
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pTriggerAmount", (int)(Trigger * 100));
                cmd.Parameters["@pTriggerAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pTriggerType", Type);
                cmd.Parameters["@pTriggerType"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@success", MySqlDbType.Bit));
                cmd.Parameters["@success"].Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                return Convert.ToBoolean(cmd.Parameters["@success"].Value);
            }
        }

        async Task BuyStock(MySqlConnection cnn, decimal cost)
        {
            decimal numStock = Math.Floor(Amount / cost); // most whole number stock that can buy
            decimal amount = numStock * cost; // total amount spent
            decimal leftover = Amount - amount;

            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "buy_trigger";

                cmd.Parameters.AddWithValue("@pUserId", User);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", StockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStockAmount", (int)(amount * 100));
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@MoneyLeftover", (int)(leftover * 100));
                cmd.Parameters["@pMoneyLeftover"].Direction = ParameterDirection.Input;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
