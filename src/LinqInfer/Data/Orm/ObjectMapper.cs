using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Data.Orm
{
    internal class ObjectMapper<T>
    {
        private readonly IDictionary<string, Action<object, object>> _mappings;

        public ObjectMapper(DataTable schema)
        {
            _mappings = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
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

        public Action<T, Type, string, object> Map
        {
            get
            {
                return (x, t, c, v) => _mappings[c](x, v);
            }
        }

        private Func<object, object> CreateConverter(DataRow col, PropertyInfo prop)
        {
            var ct = Type.GetTypeCode(col.Field<Type>(12)); // TODO: Is this consistently implemented? - probably not
            var pt = Type.GetTypeCode(prop.PropertyType);

            if (ct == pt) return (v) => v;

            return v => Convert.ChangeType(v, pt);
        }
    }
}
