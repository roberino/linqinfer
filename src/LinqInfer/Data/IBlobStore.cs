using System;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    public interface IBlobStore : IDisposable
    {
        bool Store<T>(string key, T obj) where T : IBinaryPersistable;

        T Restore<T>(string key, T obj) where T : IBinaryPersistable;

        Task<bool> StoreAsync<T>(string key, T obj) where T : IBinaryPersistable;

        Task<T> RestoreAsync<T>(string key, T obj) where T : IBinaryPersistable;
    }
}