using System;
using System.Collections.Concurrent;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal static class TypeMappingCache
    {
        private static readonly ConcurrentDictionary<Type, ITypeMapping> _mappingCache;

        static TypeMappingCache()
        {
            _mappingCache = new ConcurrentDictionary<Type, ITypeMapping>();
        }

        public static ITypeMapping GetMapping<T>()
        {
            return _mappingCache.GetOrAdd(typeof(T), t => new TypeMapping<T>());
        }
    }
}
