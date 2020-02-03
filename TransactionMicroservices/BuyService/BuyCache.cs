using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Database;
using Newtonsoft.Json;

namespace BuyService
{
    class BuyCache
    {
        private static readonly ObjectCache _cache = MemoryCache.Default;
        public static void StoreItemsInCache(string key, string ItemsToAdd)
        { 
            var cacheItemPolicy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTime.Now.AddSeconds(60),
                RemovedCallback = new CacheEntryRemovedCallback(CacheRemovedCallback)
            };
            _cache.Set(key, ItemsToAdd, cacheItemPolicy);
            Console.WriteLine("Successfully cached the buy request.");
        }

        public static string GetItemsFromCache(string key)
        {
            return _cache.Get(key) as string;
        }

        public static void RemoveItems(string key)
        {
            _cache.Remove(key);
        }

        public static void CacheRemovedCallback(CacheEntryRemovedArguments arguments)
        {
            string[] args = Convert.ToString(arguments.CacheItem).Split(",");
            string user = args[0];
            string amount = args[3];

            try
            {
                MySQL db = new MySQL();

                var balance = db.Execute($"SELECT money FROM user WHERE user = {user}");
                var userBalance = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(balance);
                double realBalance = Convert.ToDouble(userBalance[1]["money"]) / 100;
                double newBalance = (realBalance + Convert.ToInt32(amount)) * 100;
                db.ExecuteNonQuery($"UPDATE user SET money = {newBalance} WHERE userid = {user}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
