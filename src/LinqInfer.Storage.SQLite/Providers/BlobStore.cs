using System;
using System.Threading.Tasks;
using LinqInfer.Data;
using System.Data;
using LinqInfer.Storage.SQLite.Models;
using System.Linq;

namespace LinqInfer.Storage.SQLite.Providers
{
    public class BlobStore : StoreProvider, IBlobStore
    {
        public BlobStore(string dataDir = null) : base(dataDir)
        {
        }

        public override Task Setup()
        {
            _db.CreateTableFor<BlobItem>(true);
            return Task.FromResult(0);
        }

        public T Restore<T>(string key, T obj) where T : IBinaryPersistable
        {
            var blob = _db.Query<BlobItem>(x => x.Key == key, 1).ToList();

            if (blob.Any())
            {
                using (var ms = blob.First().Read())
                {
                    obj.Load(blob.First().Read());
                }
            }
            else
            {
                throw new ArgumentException("Key not found " + key);
            }

            return obj;
        }

        public Task<T> RestoreAsync<T>(string key, T obj) where T : IBinaryPersistable
        {
            return Task.FromResult(Restore(key, obj));
        }

        public bool Store<T>(string key, T obj) where T : IBinaryPersistable
        {
            var blob = new BlobItem() { Key = key, TypeName = typeof(T).FullName };

            using (var ms = blob.Write())
            {
                obj.Save(ms);
            }

            _db.Insert(new[] { blob });

            return true;
        }

        public Task<bool> StoreAsync<T>(string key, T obj) where T : IBinaryPersistable
        {
            return Task.FromResult(Store(key, obj));
        }
    }
}