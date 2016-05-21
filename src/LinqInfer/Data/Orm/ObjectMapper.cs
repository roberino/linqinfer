using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Data.Orm
{
    internal class ObjectMapper<T> : IObjectMapper<T>
    {
        private readonly IDictionary<string, Action<object, object>> _mappings;

        public ObjectMapper(DataTable schema)
        {
            _mappings = GetMappedProperties()
                .Join(schema.Rows.Cast<DataRow>(), o => o.Name.ToLowerInvariant(), i => i.Field<string>(0).ToLowerInvariant(), (o, i) => new
                {
                    prop = o,
                    col = i
                })
                .GroupBy(g => g.col.Field<string>(0))
                .ToDictionary(k => k.Key, p =>
                {
                    var prop = p.First().prop;
                    var converter = CreateConverter(p.First().col, prop);

                    return new Action<object, object>((x, v) => prop.SetValue(x, converter(v)));
                });
        }

        public void MapProperty(T instance, Type propertyType, string propertyName, object value)
        {
            _mappings[propertyName](instance, value);
        }

        protected IEnumerable<PropertyInfo> GetMappedProperties()
        {
            return typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(r => r.CanRead);
        }

        protected virtual Func<object, object> CreateConverter(DataRow col, PropertyInfo prop)
        {
            var ct = Type.GetTypeCode(col.Field<Type>(12)); // TODO: Is this consistently implemented? - probably not
            var pt = Type.GetTypeCode(prop.PropertyType);

            if (ct == pt) return (v) => v;

            return v => Convert.ChangeType(v, pt);
        }
    }
}
