using LinqInfer.Data;
using LinqInfer.Storage.SQLite.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.Providers
{
    public class BlobStore : StoreProvider, IBlobStore
    {
        public BlobStore(DirectoryInfo dataDir) : base(dataDir.FullName) { }

        public BlobStore(string dataDir = null) : base(dataDir)
        {
        }

        public async override Task Setup(bool reset = false)
        {
            await base.Setup(reset);
            await _db.CreateTableFor<BlobItem>(!reset);
        }

        public async Task<bool> Transfer<T>(string key, Stream output)
        {
            var blob = (await _db.QueryAsync<BlobItem>(x => x.Key == key, 1)).ToList();

            if (blob.Any())
            {
                using (var ms = blob.First().Read())
                {
                    await ms.CopyToAsync(output);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Delete<T>(string key)
        {
            _db.DeleteAsync<BlobItem>(x => x.Key == key).Wait();

            return true;
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

            _db.Insert(blob);

            return true;
        }

        public async Task<bool> StoreAsync<T>(string key, T obj) where T : IBinaryPersistable
        {
            var blob = new BlobItem() { Key = key, TypeName = typeof(T).FullName };

            using (var ms = blob.Write())
            {
                obj.Save(ms);
            }

            await _db.InsertAsync(blob);

            return true;
        }
    }
}