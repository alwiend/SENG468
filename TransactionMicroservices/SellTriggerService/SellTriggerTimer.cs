using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Utilities;

namespace SellTriggerService
{
    public sealed class SellTriggerTimer : IDisposable
    {
        public string User { get; }
        public string StockSymbol { get; }
        public string Type { get; }
        public decimal Amount { get; }
        public decimal Trigger { get; }

        readonly Timer timer;

        public SellTriggerTimer(string user, string ss, decimal amount, decimal trigger)
        {
            User = user;
            StockSymbol = ss;
            Type = "SELL";
            Amount = amount;
            Trigger = trigger;
            timer = new Timer() { Enabled = false };
            timer.Elapsed += Run;
        }

        public void Start()
        {
            Run(null, null);
        }

        void Run(object source, ElapsedEventArgs eventArgs)
        {
            timer.Enabled = false;
            // Check if trigger still exists
            if (!TriggerExists())
            {
                return;
            }

            ServiceConnection conn = new ServiceConnection(Constants.Server.QUOTE_SERVER);
            string response = conn.Send($"{StockSymbol},{User}", true);
            string[] args = response.Split(",");

            decimal cost = Convert.ToDecimal(args[0]);
            if(cost < Trigger)
            {
                // Run trigger again
                timer.Interval = 60000 - (Unix.TimeStamp - Convert.ToInt64(args[3]));
                timer.Enabled = true;
                return;
            }
            SellStock(cost);
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
            db.ExecuteNonQuery($"UPDATE user SET money=money+{(int)(amount*100)} WHERE userid='{User}'");
            string query = $"INSERT INTO stocks (userid, stock, price) " +
                $"VALUES ('{User}', '{StockSymbol}', {(int)(leftover * 100)}) " +
                $"ON DUPLICATE KEY UPDATE price = price + {(int)(leftover * 100)}";
            db.ExecuteNonQuery(query);
            query = $"DELETE FROM triggers " +
                $"WHERE userid='{User}' AND stock='{StockSymbol}' AND triggerType='SELL'";
            db.ExecuteNonQuery(query);
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}
