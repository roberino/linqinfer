using System;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    public interface IBlobStore : IDisposable
    {
        /// <summary>
        /// Stores a binary object using the supplied key
        /// </summary>
        bool Store<T>(string key, T obj) where T : IBinaryPersistable;

        /// <summary>
        /// Restores an object to it's previous state using state information stored against the supplied key
        /// </summary>
        T Restore<T>(string key, T obj) where T : IBinaryPersistable;

        /// <summary>
        /// Asyncronously stores a binary object using the supplied key
        /// </summary>
        Task<bool> StoreAsync<T>(string key, T obj) where T : IBinaryPersistable;
        /// <summary>
        /// Asyncronously restores an object to it's previous state using state information stored against the supplied key
        /// </summary>
        Task<T> RestoreAsync<T>(string key, T obj) where T : IBinaryPersistable;
    }
}