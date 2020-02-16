using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Utilities;

namespace BuyTriggerService
{
    public class BuyTriggerTimer
    {
        public string User { get; }
        public string StockSymbol { get; }
        public string Type { get; }
        public decimal Amount { get; }
        public decimal Trigger { get; }

        Timer _timer;

        public BuyTriggerTimer(string user, string ss, decimal amount, decimal trigger)
        {
            User = user;
            StockSymbol = ss;
            Type = "BUY";
            Amount = amount;
            Trigger = trigger;
            _timer = new Timer() { Enabled = false };
            _timer.Elapsed += Run;
        }

        public void Start()
        {
            Run(null, null);
        }

        void Run(object source, ElapsedEventArgs eventArgs)
        {
            _timer.Enabled = false;
            // Check if trigger still exists
            if (!TriggerExists())
            {
                return;
            }

            ServiceConnection conn = new ServiceConnection(Constants.Server.QUOTE_SERVER);
            string response = conn.Send($"{StockSymbol},{User}", true);
            string[] args = response.Split(",");

            decimal cost = Convert.ToDecimal(args[0]);
            if(cost > Trigger)
            {
                // Run trigger again
                _timer.Interval = 60000 - (Unix.TimeStamp - Convert.ToInt64(args[3]));
                _timer.Enabled = true;
                return;
            }
            BuyStock(cost);
        }

        bool TriggerExists()
        {
            MySQL db = new MySQL();
            string query = $"SELECT 1 FROM triggers " +
                $"WHERE userid='{User}' AND stock='{StockSymbol}' AND triggerType='BUY' " +
                $"AND amount={(int)(Amount * 100)} AND triggerAmount={(int)(Trigger * 100)}";
            var hasTrigger = db.Execute(query);
            var triggerObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasTrigger);
            return triggerObject.Length == 1;
        }

        void BuyStock(decimal cost)
        {
            decimal numStock = Math.Floor(Amount / cost); // most whole number stock that can buy
            decimal amount = numStock * cost; // total amount spent
            decimal leftover = Amount - amount;

            MySQL db = new MySQL();
            // Put leftover amount back in users account
            db.ExecuteNonQuery($"UPDATE user SET money = money+{(int)(leftover*100)} WHERE userid='{User}'");
            string query = $"INSERT INTO stocks (userid, stock, price) " +
                $"VALUES ('{User}', '{StockSymbol}', {(int)(amount * 100)}) " +
                $"ON DUPLICATE KEY UPDATE price = price + {(int)(amount * 100)}";
            db.ExecuteNonQuery(query);
            query = $"DELETE FROM triggers " +
                $"WHERE userid='{User}' AND stock='{StockSymbol}' AND triggerType='BUY'";
            db.ExecuteNonQuery(query);
        }

    }
}
