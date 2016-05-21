using LinqInfer.Data.Orm;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

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
            return GetMappedProperties().Select(p => p.GetValue(instance)).ToArray();
        }

        protected override Func<object, object> CreateConverter(DataRow col, PropertyInfo prop)
        {
            return x => _translator.ConvertToClrValue(x, prop.PropertyType);
        }
    }
}