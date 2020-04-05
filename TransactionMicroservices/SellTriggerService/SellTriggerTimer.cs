﻿using Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Concurrent;
using System.Linq;

namespace TransactionServer.Services.SellTrigger
{
    public sealed class SellTriggerTimer : IDisposable
    {
        private readonly string _stockSymbol;
        private int _cost = int.MaxValue;
        private readonly ConcurrentDictionary<string, SellTrigger> _users;

        private static readonly ConcurrentDictionary<string, SellTriggerTimer> _timers = new ConcurrentDictionary<string, SellTriggerTimer>();
        private static readonly object _timerLock = new object();
        private static readonly IAuditWriter _auditWriter = new AuditWriter();
        readonly static ConcurrentDictionary<string, ConnectedServices> clientConnections = new ConcurrentDictionary<string, ConnectedServices>();

        
        public SellTriggerTimer(string ss)
        {
            _stockSymbol = ss;
            _users = new ConcurrentDictionary<string, SellTrigger>();
        }

        public void Dispose()
        {
            if (clientConnections.TryGetValue(_stockSymbol, out ConnectedServices svc))
            {
                svc.Dispose();
            }
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
                var _ = timer.Start();
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
            int interval = 0;
            while (!_users.IsEmpty)
            {
                try
                {
                    var firstUser = _users.Values.First();
                    string response = await GetStock(firstUser.User, firstUser.StockSymbol).ConfigureAwait(false);
                    if (response == null)
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                        continue;
                    }
                    string[] args = response.Split(",");

                    _cost = Convert.ToInt32(args[0]);
                    var triggered = _users.Values.Where(t => t.Trigger <= _cost);
                    if (triggered.Any())
                    {
                        await SellStockAndRemoveUserTrigger(triggered).ConfigureAwait(false);
                    }

                    interval = (int)(60000 - (Unix.TimeStamp - Convert.ToInt64(args[1])));
                    await Task.Delay(interval).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogDebugEvent(null, ex.Message);
                    await Task.Delay(5000).ConfigureAwait(false);
                }
            }
            _timers.TryRemove(_stockSymbol, out _);
            Dispose();
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
            var delay = 500;
            UserCommandType cmd = new UserCommandType
            {
                server = Service.SELL_TRIGGER_SET_SERVICE.Abbr,
                command = commandType.SET_BUY_TRIGGER,
                stockSymbol = stockSymbol,
                username = username
            };

            try
            {
                string result = null;
                do
                {
                    ConnectedServices cs = clientConnections.GetOrAdd(_stockSymbol, new ConnectedServices());
                    ServiceConnection conn = await cs.GetServiceConnectionAsync(Service.QUOTE_SERVICE).ConfigureAwait(false);
                    if (conn == null)
                    {
                        throw new Exception("Failed to connect to service");
                    }
                    result = await conn.SendAsync(cmd, true).ConfigureAwait(false);
                    if (result == null)
                        await Task.Delay(delay).ConfigureAwait(false); // Short delay before tring again
                } while (result == null);

                return result;
            }
            catch (Exception ex)
            {
                LogDebugEvent(cmd, ex.Message);
                return null;
            }
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
            _auditWriter.WriteRecord(bug);
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
