using LinqInfer.Storage.SQLite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal class TypeMapping<T> : ITypeMapping
    {
        private static readonly SqlTypeTranslator _translator = new SqlTypeTranslator();

        private Lazy<IDictionary<string, ColumnDef>> _cols;

        public TypeMapping()
        {
            _cols = new Lazy<IDictionary<string, ColumnDef>>(() => GetMappedProperties()
                  .Select(p => new ColumnDef()
                  {
                      Name = p.Name,
                      DataType = _translator.TranslateToSqlTypeName(p.PropertyType),
                      PrimaryKey = IsPrimaryKey(p),
                      ClrType = p.PropertyType,
                      MappedProperty = p                       
                  })
                  .ToDictionary(x => x.Name));
        }

        public string TableName
        {
            get
            {
                return typeof(T).Name;
            }
        }

        public void SetPrimaryKey(object item, long id)
        {
            var col = _cols.Value.SingleOrDefault(c => c.Value.PrimaryKey);

            if (col.Key == null) throw new InvalidOperationException("No primary key defined");

            col.Value.MappedProperty.SetValue(item, id);
        }

        public IEnumerable<string> GetSqlFieldNames()
        {
            return GetMappedProperties().Select(p => p.Name);
        }

        public IDictionary<string, ColumnDef> GetSqlFieldDef()
        {
            return _cols.Value;
        }

        public bool IsPrimaryKey(PropertyInfo prop)
        {
            return prop.PropertyType == typeof(long) && prop.Name == "Id";
        }

        public bool HasPrimaryKey
        {
            get
            {
                return GetSqlFieldDef().Any(c => c.Value.PrimaryKey);
            }
        }

        public IEnumerable<PropertyInfo> GetMappedProperties()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && _translator.CanConvertToSql(p.PropertyType));
        }
    }
}
