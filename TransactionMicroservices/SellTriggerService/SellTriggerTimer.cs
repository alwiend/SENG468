using Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Concurrent;
using System.Linq;

namespace SellTriggerService
{
    public class SellTriggerTimer
    {
        private string _stockSymbol;
        private int _cost = int.MaxValue;
        private readonly ConcurrentDictionary<string, SellTrigger> _users;

        private static readonly ConcurrentDictionary<string, SellTriggerTimer> _timers = new ConcurrentDictionary<string, SellTriggerTimer>();
        private static readonly object _timerLock = new object();
        private static readonly IAuditWriter _auditWriter = new AuditWriter();

        
        public SellTriggerTimer(string ss)
        {
            _stockSymbol = ss;
            _users = new ConcurrentDictionary<string, SellTrigger>();
        }

        public static async Task<string> StartOrUpdateTimer(UserCommandType command)
        {
            SellTriggerTimer timer;
            bool newTimer;
            lock (_timerLock)
            {
                newTimer = !_timers.TryGetValue(command.stockSymbol, out timer);

                if (newTimer)
                {
                    if (command.fundsSpecified)
                    {
                        return "Set sell amount before creating trigger";
                    }
                    timer = new SellTriggerTimer(command.stockSymbol);
                    if (!_timers.TryAdd(command.stockSymbol, timer))
                    {
                        LogDebugEvent(command, "Error adding timer");
                    }
                }
            }

            var msg = await timer.AddOrUpdateUser(command).ConfigureAwait(false);

            if (newTimer)
            {
                timer.Start();
            }
            return msg;
        }

        public static SellTrigger RemoveUserTrigger(string user, string ss)
        {
            if (_timers.TryGetValue(ss, out SellTriggerTimer timer))
            {
                return timer.Remove(user);
            }
            return null;
        }

        public async Task<string> AddOrUpdateUser(UserCommandType command)
        {
            if (_users.TryGetValue(command.username, out SellTrigger trigger))
            {
                if (!command.fundsSpecified) // Using fundsSpecified to indicate if it was SET_SELL_AMOUNT (false) or SET_SELL_TRIGGER (true)
                {
                    return "Sell amount already set for this stock";
                }
                if ((int)(command.funds) == trigger.Trigger)
                {
                    return "Trigger amount already set for this stock";
                }
                if ((int)command.funds > trigger.Amount)
                {
                    return "Trigger amount can not be greater than Sell amount";
                }
                trigger.Trigger = (int)(command.funds);
                if (trigger.Trigger <= _cost)
                {
                    await SellStockAndRemoveUserTrigger(new SellTrigger[] { trigger }).ConfigureAwait(false);
                }
                return null; // Signals success, output in Service
            }
            if (command.fundsSpecified)
            {
                return "Set sell amount before creating trigger";
            }

            trigger = new SellTrigger(command.username, command.stockSymbol, (int)(command.funds));
            while (!_users.TryAdd(command.username, trigger)) ;

            return null; // Signals success, output in Service
        }

        public SellTrigger Remove(string user)
        {
            if (!_users.ContainsKey(user))
            {
                return null;
            }
            SellTrigger trigger;
            while (!_users.TryRemove(user, out trigger)) ;
            return trigger;
        }

        async Task Start()
        {
            while (!_users.IsEmpty)
            {
                try
                {
                    var firstUser = _users.Values.First();
                    string response = await GetStock(firstUser.User, firstUser.StockSymbol).ConfigureAwait(false);
                    string[] args = response.Split(",");

                    _cost = Convert.ToInt32(args[0]);
                    var triggered = _users.Values.Where(t => t.Trigger <= _cost);
                    if (triggered.Count() > 0)
                    {
                        await SellStockAndRemoveUserTrigger(triggered).ConfigureAwait(false);
                    }

                    int interval = (int)(60000 - (Unix.TimeStamp - Convert.ToInt64(args[1])));
                    await Task.Delay(interval).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogDebugEvent(null, ex.Message);
                    await Task.Delay(5000).ConfigureAwait(false);
                }
            }
            _timers.TryRemove(_stockSymbol, out _);
        }

        private async Task SellStockAndRemoveUserTrigger(IEnumerable<SellTrigger> triggered)
        {
            MySQL db = new MySQL();
            // Sell stock
            await db.PerformTransaction(async (cnn) =>
            {
                foreach (SellTrigger trigger in triggered)
                {
                    await SellStock(cnn, trigger).ConfigureAwait(false);
                    RemoveUserTrigger(trigger.User, _stockSymbol);
                }
                return true;
            }).ConfigureAwait(false);
        }

        async Task<bool> SellStock(MySqlConnection cnn, SellTrigger trigger)
        {
            int numStock = trigger.Amount / _cost; // most whole number stock that can Sell
            int amount = numStock * _cost; // total amount spent
            int leftover = trigger.Amount - amount;

            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "sell_trigger";

                cmd.Parameters.AddWithValue("@pUserId", trigger.User);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStock", trigger.StockSymbol);
                cmd.Parameters["@pStock"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStockAmount", amount);
                cmd.Parameters["@pStockAmount"].Direction = ParameterDirection.Input;
                cmd.Parameters.AddWithValue("@pStockLeftover", leftover);
                cmd.Parameters["@pStockLeftover"].Direction = ParameterDirection.Input;

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            return true;
        }

        async Task<string> GetStock(string username, string stockSymbol)
        {
            UserCommandType cmd = new UserCommandType
            {
                server = Constants.Server.WEB_SERVER.Abbr,
                command = commandType.SET_SELL_TRIGGER,
                stockSymbol = stockSymbol,
                username = username
            };
            ServiceConnection conn = new ServiceConnection(Constants.Service.QUOTE_SERVICE);
            return await conn.Send(cmd, true).ConfigureAwait(false);
        }

        static void LogDebugEvent(UserCommandType command, string err)
        {
            DebugType bug = new DebugType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = "STSVC",
                debugMessage = err
            };
            if (command != null)
            {
                bug.transactionNum = command.transactionNum;
                bug.command = command.command;
                bug.fundsSpecified = command.fundsSpecified;

                if (command.fundsSpecified)
                    bug.funds = command.funds / 100m;
            }
            _auditWriter.WriteRecord(bug).ConfigureAwait(false);
        }
    }
    
    public class SellTrigger
    {
        public string User { get; }
        public string StockSymbol { get; }
        public string Type { get; }
        public int Amount { get; }
        public int? Trigger { get; set; }

        public SellTrigger(string user, string ss, int amount)
        {
            User = user;
            StockSymbol = ss;
            Type = "Sell";
            Amount = amount;
            Trigger = null;
        }

        public bool IsTriggered(int price)
        {
            return price >= Trigger;
        }
    }
}
