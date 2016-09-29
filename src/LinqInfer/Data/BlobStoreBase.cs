using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    /// <summary>
    /// Base class for implementing <see cref="IBlobStore"/>
    /// </summary>
    public abstract class BlobStoreBase : IBlobStore
    {

        protected const string KeyDelimitter = "$";

        private bool _isDisposed;

        public abstract Task<IEnumerable<string>> ListKeys<T>();

        public virtual bool Store<T>(string key, T obj) where T : IBinaryPersistable
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name);

            var qkey = GetKey(key, obj);

            using (var ms = GetWriteStream(qkey))
            {
                obj.Save(ms);

                ms.Flush();

                OnWrite(qkey, ms);
            }

            return true;
        }

        public virtual T Restore<T>(string key, T obj) where T : IBinaryPersistable
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name);

            var qkey = GetKey(key, obj);

            using (var ms = GetReadStream(qkey))
            {
                obj.Load(ms);
            }

            return obj;
        }

        public virtual Task<bool> StoreAsync<T>(string key, T obj) where T : IBinaryPersistable
        {
            return Task.FromResult(Store(key, obj));
        }

        public virtual Task<T> RestoreAsync<T>(string key, T obj) where T : IBinaryPersistable
        {
            return Task.FromResult(Restore(key, obj));
        }

        public async Task<bool> Transfer<T>(string key, Stream output)
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name);

            var qkey = GetKey<T>(key);

            using (var ms = GetReadStream(qkey))
            {
                await ms.CopyToAsync(output);
            }

            return true;
        }

        public bool Delete<T>(string key)
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name);

            var fullKey = GetKey<T>(key);

            return RemoveBlob(fullKey);
        }

        public virtual void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        protected abstract bool RemoveBlob(string key);

        protected virtual void OnWrite(string key, Stream stream)
        {
        }

        protected abstract Stream GetReadStream(string key);

        protected abstract Stream GetWriteStream(string key);

        protected virtual string GetKey<T>(string key, T obj = default(T))
        {
            return GetTypeKeyPart(obj) + KeyDelimitter + key;
        }

        protected string GetTypeKeyPart<T>(T obj = default(T))
        {
            return (obj == null ? typeof(T).FullName : obj.GetType().FullName);
        }

        protected class Blob
        {
            public DateTime Created;
            public byte[] Data;
        }
    }
}