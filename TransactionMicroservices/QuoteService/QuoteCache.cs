using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionServer.Services.Quote
{
    public class QuoteCache<T>
    {
        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

        public async Task<T> GetOrCreate(object key, Func<Task<T>> createItem, Func<T,bool> validate)
        {
            if (!_cache.TryGetValue(key, out T cacheEntry) || !validate(cacheEntry))
            {
               cacheEntry = await CreateItem(key, createItem, validate);
            }

            return cacheEntry;
        }

        private async Task<T> CreateItem(object key, Func<Task<T>> createItem, Func<T, bool> validate)
        {
            T cacheEntry;
            SemaphoreSlim l = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
            await l.WaitAsync().ConfigureAwait(false);
            try
            {
                // Another call may have put it in the cache, but double check that its not the old one
                if (!_cache.TryGetValue(key, out cacheEntry) || !validate(cacheEntry))
                {
                    // Data still not in cache, fetch
                    cacheEntry = await createItem().ConfigureAwait(false);
                    _cache.Set(key, cacheEntry);
                }
            }
            finally
            {
                l.Release();
            }
            return cacheEntry;

        }
    }
}
