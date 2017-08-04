using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace LinqInfer.Microservices
{
    internal class DefaultCache : ICache
    {
        private readonly ConcurrentDictionary<object, CacheEntry> _cacheStore;
        private readonly TimeSpan _cacheItemLifespan;

        public DefaultCache(TimeSpan? cacheItemLifespan = null)
        {
            _cacheItemLifespan = cacheItemLifespan.GetValueOrDefault(TimeSpan.FromSeconds(120));

            if (_cacheItemLifespan.TotalMilliseconds <= 10) throw new ArgumentOutOfRangeException();

            _cacheStore = new ConcurrentDictionary<object, CacheEntry>();

            var time = new Timer(x =>
            {
                RemoveStaleData();
            }, true, 10, (int)_cacheItemLifespan.TotalMilliseconds / 4);
        }

        public T Get<T>(object key)
        {
            if (_cacheStore.TryGetValue(key, out CacheEntry v) && v.Data is T && !v.Expired)
            {
                return (T)v.Data;
            }

            return default(T);
        }

        public void Set<T>(object key, T item)
        {
            _cacheStore[key] = new CacheEntry() { Data = item };
        }

        private void RemoveStaleData()
        {
            var expiredTime = DateTime.UtcNow - _cacheItemLifespan;

            foreach (var item in _cacheStore.Where(c => c.Value.Created < expiredTime))
            {
                item.Value.Expired = true;
            }

            foreach (var key in _cacheStore.Where(c => c.Value.Expired).Select(k => k.Key).ToList())
            {
                _cacheStore.TryRemove(key, out CacheEntry e);
            }
        }

        private class CacheEntry
        {
            public DateTime Created { get; set; } = DateTime.UtcNow;
            public object Data { get; set; }
            public bool Expired { get; set; }
        }
    }
}