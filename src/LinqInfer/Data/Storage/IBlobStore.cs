using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data.Storage
{
    /// <summary>
    /// Represents a storage facility for binary objects
    /// using a type key pair to locate the blob
    /// </summary>
    public interface IBlobStore : IDisposable
    {
        /// <summary>
        /// Transfers the blob to a stream
        /// </summary>
        Task<bool> Transfer<T>(string key, Stream output);

        /// <summary>
        /// Gets a list of keys for a particullar type
        /// </summary>
        Task<IEnumerable<string>> ListKeys<T>();

        /// <summary>
        /// Deletes an item stored against the supplied key
        /// </summary>
        bool Delete<T>(string key);

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