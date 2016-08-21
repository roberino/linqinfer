using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Data.Orm
{
    public class ObjectMapper<T> : IObjectMapper<T>
    {
        private readonly IDictionary<string, Action<object, object>> _mappings;

        public ObjectMapper(DataTable schema)
        {
            _mappings = GetMappedProperties()
                .Join(schema.Rows.Cast<DataRow>(), o => o.Name.ToLowerInvariant(), i => i.Field<string>(0).ToLowerInvariant(), (o, i) => new
                {
                    mapping = o,
                    col = i
                })
                .GroupBy(g => g.col.Field<string>(0))
                .ToDictionary(k => k.Key, p =>
                {
                    var mapping = p.First().mapping;
                    var converter = CreateConverter(p.First().col, mapping);

                    return new Action<object, object>((x, v) => mapping.MappedProperty.SetValue(x, converter(v)));
                });
        }

        public virtual void MapProperty(T instance, Type fieldType, string columnName, object value)
        {
            _mappings[columnName](instance, value);
        }

        protected virtual IEnumerable<ColumnMapping> GetMappedProperties()
        {
            return typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(r => r.CanRead && r.CanWrite)
                .Select(p => {
                    var innerType = p.PropertyType.GetNullableTypeType();

                    return new ColumnMapping()
                    {
                        ClrType = innerType ?? p.PropertyType,
                        MappedProperty = p,
                        Name = p.Name,
                        Nullable = innerType != null
                    };
                });
        }

        protected virtual Func<object, object> CreateConverter(DataRow col, ColumnMapping mapping)
        {
            var ct = Type.GetTypeCode(col.Field<Type>(12)); // TODO: Is this consistently implemented? - probably not
            var pt = Type.GetTypeCode(mapping.ClrType);

            if (ct == pt) return (v) => v;

            return v => Convert.ChangeType(v, pt);
        }
    }
}
