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

        public async Task<T> GetOrCreate(object key, Func<Task<T>> createItem)
        {
            if (!_cache.TryGetValue(key, out T cacheEntry))
            {
                SemaphoreSlim l = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
                await l.WaitAsync().ConfigureAwait(false);
                try
                {
                    if(!_cache.TryGetValue(key, out cacheEntry))
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
            }

            return cacheEntry;
        }
    }
}
