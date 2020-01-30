using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace BuyService
{
    class BuyCache
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;
        public static void StoreItemsInCache(string key, List<string> ItemsToAdd)
        {
            var cacheItemPolicy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTime.Now.AddSeconds(60)
            };
            _cache.Add(key, ItemsToAdd, cacheItemPolicy);
        }

        public static List<string> GetItemsFromCache(string key)
        {
            if (!_cache.Contains(key))
            {
                List<string> NA = new List<string>();
                NA.Add("Transaction not found.");
                StoreItemsInCache(key, NA);
            }
            return _cache.Get(key) as List<string>;
        }

        public static void RemoveItems(string key)
        {
            _cache.Remove(key);
        }
    }
}
