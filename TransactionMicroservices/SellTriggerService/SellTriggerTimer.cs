using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;

namespace SellTriggerService
{
    public sealed class SellTriggerTimer
    {
        public string User { get; }
        public string StockSymbol { get; }
        public string Type { get; }
        public decimal Amount { get; }
        public decimal Trigger { get; }

        public SellTriggerTimer(string user, string ss, decimal amount, decimal trigger)
        {
            User = user;
            StockSymbol = ss;
            Type = "SELL";
            Amount = amount;
            Trigger = trigger;
        }

        public async Task Start()
        {
            await Run().ConfigureAwait(false);
        }

        async Task Run()
        {
            while (true)
            {
                // Check if trigger still exists
                if (!TriggerExists())
                {
                    return;
                }

                ServiceConnection conn = new ServiceConnection(Constants.Server.QUOTE_SERVER);
                string response = await conn.Send($"{StockSymbol},{User}", true).ConfigureAwait(false);
                string[] args = response.Split(",");

                decimal cost = Convert.ToDecimal(args[0]);
                if (cost < Trigger)
                {
                    // Run trigger again

                    int interval = (int)(60000 - (Unix.TimeStamp - Convert.ToInt64(args[3])));
                    await Task.Delay(interval).ConfigureAwait(false);
                } else
                {
                    SellStock(cost);
                    return;
                }
            }
        }

        bool TriggerExists()
        {
            MySQL db = new MySQL();
            string query = $"SELECT 1 FROM triggers " +
                $"WHERE userid='{User}' AND stock='{StockSymbol}' AND triggerType='SELL' " +
                $"AND amount={(int)(Amount * 100)} AND triggerAmount={(int)(Trigger * 100)}";
            var hasTrigger = db.Execute(query);
            var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
            return triggerObject.Length == 1;
        }

        void SellStock(decimal cost)
        {
            decimal numStock = Math.Floor(Amount / cost); // most whole number stock that can buy
            decimal amount = numStock * cost; // total amount spent
            decimal leftover = Amount - amount;

            MySQL db = new MySQL();
            // Put leftover amount back in users account
            db.ExecuteNonQuery($"UPDATE user SET money=money+{(int)(amount * 100)} WHERE userid='{User}'");
            string query = $"INSERT INTO stocks (userid, stock, price) " +
                $"VALUES ('{User}', '{StockSymbol}', {(int)(leftover * 100)}) " +
                $"ON DUPLICATE KEY UPDATE price = price + {(int)(leftover * 100)}";
            db.ExecuteNonQuery(query);
            query = $"DELETE FROM triggers " +
                $"WHERE userid='{User}' AND stock='{StockSymbol}' AND triggerType='SELL'";
            db.ExecuteNonQuery(query);
        }
    }
}
