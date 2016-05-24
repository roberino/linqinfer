using LinqInfer.Data.Orm;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    class SQLiteObjectMapper<T> : ObjectMapper<T>
    {
        protected readonly SqlTypeTranslator _translator;

        public SQLiteObjectMapper(DataTable schema) : base(schema)
        {
            _translator = new SqlTypeTranslator();
        }

        public object[] GetValues(T instance)
        {
            return GetMappedProperties().Select(p => p.MappedProperty.GetValue(instance)).ToArray();
        }

        protected override Func<object, object> CreateConverter(DataRow col, ColumnMapping prop)
        {
            return x => _translator.ConvertToClrValue(x, prop.ClrType, prop.Nullable);
        }

        protected override IEnumerable<ColumnMapping> GetMappedProperties()
        {
            return TypeMappingCache.GetMapping<T>().GetSqlFieldDef().Values;
        }
    }
}