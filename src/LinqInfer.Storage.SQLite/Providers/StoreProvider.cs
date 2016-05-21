using LinqInfer.Storage.SQLite.DataAccess;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.Providers
{
    public abstract class StoreProvider : IDisposable
    {
        internal readonly SQLiteDbController _db;
        
        internal protected StoreProvider(string dataDir)
        {
            _db = new SQLiteDbController(GetType().Name, dataDir ?? Environment.CurrentDirectory);
        }

        public virtual Task Setup()
        {
            return Task.FromResult(0);
        }

        public virtual void Dispose()
        {
            _db.Dispose();
        }

        public void Destroy()
        {
            _db.Destroy();
        }
    }
}
