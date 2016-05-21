using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    public class InMemoryBlobStore : IBlobStore
    {
        private readonly ConcurrentDictionary<string, Blob> _data;

        private bool _isDisposed;

        public InMemoryBlobStore()
        {
            _data = new ConcurrentDictionary<string, Blob>();
        }

        public bool Store<T>(string key, T obj) where T : IBinaryPersistable
        {
            if (_isDisposed) throw new ObjectDisposedException(typeof(InMemoryBlobStore).Name);

            using (var ms = new MemoryStream())
            {
                var blob = new Blob()
                {
                    Created = DateTime.UtcNow,
                    Data = ms.ToArray()
                };

                _data[GetKey<T>(key)] = blob;
            }

            return true;
        }

        public T Restore<T>(string key, T obj) where T : IBinaryPersistable
        {
            if (_isDisposed) throw new ObjectDisposedException(typeof(InMemoryBlobStore).Name);

            var blob = _data[GetKey<T>(key)];

            using(var ms = new MemoryStream(blob.Data))
            {
                obj.Load(ms);
            }

            return obj;
        }

        public Task<bool> StoreAsync<T>(string key, T obj) where T : IBinaryPersistable
        {
            return Task.FromResult(Store(key, obj));
        }

        public Task<T> RestoreAsync<T>(string key, T obj) where T : IBinaryPersistable
        {
            return Task.FromResult(Restore(key, obj));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                _data.Clear();
            }
        }

        private string GetKey<T>(string key)
        {
            return typeof(T).FullName + "$" + key;
        }

        private class Blob
        {
            public DateTime Created;
            public byte[] Data;
        }
    }
}
