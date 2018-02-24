using LinqInfer.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
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
            DebugOutput.Log("Caching parameters: {0}", parameters);

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

        public IEnumerable<NetworkParameters> Get<T>(int inputVectorSize, int outputSize)
        {
            ConcurrentDictionary<NetworkParameters, double> p;

            if (_store.TryGetValue(typeof(T), out p))
            {
                var  parameters = p
                    .Where(i => i.Key.InputVectorSize == inputVectorSize && i.Key.OutputVectorSize == outputSize)
                    .OrderBy(x => x.Value).Select(x => x.Key)
                    .ToList();

                DebugOutput.Log("{0} items retrieved from cache", parameters.Count);

                return parameters;
            }

            DebugOutput.Log("No items for type found cache");

            return Enumerable.Empty<NetworkParameters>();
        }
    }
}