using Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
            while (true)
            {

                // Check if trigger still exists
                if (!await TriggerExists().ConfigureAwait(false))
                {
                    return;
                }

                string response = await GetStock(User, StockSymbol).ConfigureAwait(false);
                string[] args = response.Split(",");

                decimal cost = Convert.ToDecimal(args[0]);
                if (cost > Trigger)
                {
                    // Run trigger again
                    int interval = (int)(60000 - (Unix.TimeStamp - Convert.ToInt64(args[1])));
                    await Task.Delay(interval).ConfigureAwait(false);
                } else
                {
                    await BuyStock(cost).ConfigureAwait(false);
                    return;
                }
            }
        }

        async Task<bool> TriggerExists()
        {
            MySQL db = new MySQL();
            string query = $"SELECT 1 FROM triggers " +
                $"WHERE userid='{User}' AND stock='{StockSymbol}' AND triggerType='BUY' " +
                $"AND amount={(int)(Amount * 100)} AND triggerAmount={(int)(Trigger * 100)}";
            var triggerObject = await db.ExecuteAsync(query).ConfigureAwait(false);
            return triggerObject.Length == 1;
        }

        async Task BuyStock(decimal cost)
        {
            decimal numStock = Math.Floor(Amount / cost); // most whole number stock that can buy
            decimal amount = numStock * cost; // total amount spent
            decimal leftover = Amount - amount;

            MySQL db = new MySQL();
            // Put leftover amount back in users account
            await db.ExecuteNonQueryAsync($"UPDATE user SET money = money+{(int)(leftover*100)} WHERE userid='{User}'").ConfigureAwait(false);
            string query = $"INSERT INTO stocks (userid, stock, price) " +
                $"VALUES ('{User}', '{StockSymbol}', {(int)(amount * 100)}) " +
                $"ON DUPLICATE KEY UPDATE price = price + {(int)(amount * 100)}";
            await db.ExecuteNonQueryAsync(query).ConfigureAwait(false);
            query = $"DELETE FROM triggers " +
                $"WHERE userid='{User}' AND stock='{StockSymbol}' AND triggerType='BUY'";
            await db.ExecuteNonQueryAsync(query).ConfigureAwait(false);
        }

        async Task<string> GetStock(string username, string stockSymbol)
        {
            UserCommandType cmd = new UserCommandType
            {
                server = Constants.Server.WEB_SERVER.Abbr,
                command = commandType.SET_BUY_TRIGGER,
                stockSymbol = stockSymbol,
                username = username
            };
            ServiceConnection conn = new ServiceConnection(Constants.Service.QUOTE_SERVICE);
            return await conn.Send(cmd, true).ConfigureAwait(false);
        }
    }
}
