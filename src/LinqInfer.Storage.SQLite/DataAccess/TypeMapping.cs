using LinqInfer.Data.Orm;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal class TypeMapping<T> : ITypeMapping
    {
        private static readonly SqlTypeTranslator _translator = new SqlTypeTranslator();

        private IDictionary<string, ColumnMapping> _cols;

        public TypeMapping()
        {
            int i = 0;

            _cols = GetMappedProperties()
                  .Select(p => {

                      var innerNullType = p.PropertyType.GetNullableTypeType();
                      var innerType = innerNullType ?? p.PropertyType;

                      return new ColumnMapping()
                      {
                          Name = p.Name,
                          QualifiedColumnName = GetQualifiedName(p.Name),
                          DataType = _translator.TranslateToSqlTypeName(innerType),
                          PrimaryKey = IsPrimaryKey(p),
                          ClrType = innerType,
                          Nullable = innerNullType != null,
                          MappedProperty = p
                      };
                  })
                  .ToDictionary(x => x.Name);

            _cols.Values
                .OrderBy(x => x.PrimaryKey ? 0 : 1)
                .ThenBy(x => x.Name).Select(c =>
                {
                    c.Index = i++;
                    return c;
                }).ToList();
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
            var col = _cols.SingleOrDefault(c => c.Value.PrimaryKey);

            if (col.Key == null) throw new InvalidOperationException("No primary key defined");

            col.Value.MappedProperty.SetValue(item, id);
        }

        public IEnumerable<string> GetSqlFieldNames()
        {
            return GetMappedProperties().Select(p => p.Name);
        }

        public IDictionary<string, ColumnMapping> GetSqlFieldDef()
        {
            return _cols;
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

        private string GetQualifiedName(string name)
        {
            ReservedKeywords keyword;

            if (Enum.TryParse(name, true, out keyword))
            {
                return "\"" + name + "\"";
            }

            return name;
        }
    }
}
