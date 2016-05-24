using System.Collections.Generic;
using System.Reflection;
using LinqInfer.Data.Orm;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal interface ITypeMapping
    {
        void SetPrimaryKey(object item, long id);

        IEnumerable<PropertyInfo> GetMappedProperties();
        IDictionary<string, ColumnMapping> GetSqlFieldDef();
        IEnumerable<string> GetSqlFieldNames();
        bool HasPrimaryKey { get; }
        bool IsPrimaryKey(PropertyInfo prop);
        string TableName { get; }
    }
}