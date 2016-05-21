using LinqInfer.Data.Orm;
using System.Data;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal class SQLiteObjectMapperFactory : IObjectMapperFactory
    {
        public IObjectMapper<T> Create<T>(DataTable schema)
        {
            return new SQLiteObjectMapper<T>(schema);
        }
    }
}
