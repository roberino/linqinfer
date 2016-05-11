using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal class NetworkParameterCache
    {
        private const int MaxBucketSize = 10;

        public static NetworkParameterCache DefaultCache { get; } = new NetworkParameterCache();

        private ConcurrentDictionary<Type, ConcurrentDictionary<NetworkParameters, double>> _store;

        public NetworkParameterCache()
        {
            _store = new ConcurrentDictionary<Type, ConcurrentDictionary<NetworkParameters, double>>();
        }

        public NetworkParameters Store<T>(NetworkParameters parameters, double rating)
        {
            _store.AddOrUpdate(typeof(T),
                k => new ConcurrentDictionary<NetworkParameters, double>()
                {
                    [parameters] = rating
                },
                (t, l) =>
                {
                    l[parameters] = rating;

                    if(l.Count > MaxBucketSize)
                    {
                        return new ConcurrentDictionary<NetworkParameters, double>(l.OrderBy(x => x.Value).Take(MaxBucketSize));
                    }

                    return l;
                });
            return parameters;
        }

        public IEnumerable<NetworkParameters> Get<T>()
        {
            ConcurrentDictionary<NetworkParameters, double> p;

            if (_store.TryGetValue(typeof(T), out p))
            {
                return p.OrderBy(x => x.Value).Select(x => x.Key);
            }

            return Enumerable.Empty<NetworkParameters>();
        }
    }
}