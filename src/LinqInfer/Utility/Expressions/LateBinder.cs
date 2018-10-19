using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class LateBinder
    {
        readonly IDictionary<Type, IDictionary<string, Func<object>>> _typeMap;
        readonly MethodInfo _propertyMethod;

        public LateBinder()
        {
            _typeMap = new Dictionary<Type, IDictionary<string, Func<object>>>();
            _propertyMethod = GetType().GetMethod(nameof(GetProperty), BindingFlags.Public | BindingFlags.Instance);
        }

        public Expression Bind(Expression target, Token property, Expression fallback)
        {
            var typedMethod = _propertyMethod.MakeGenericMethod(fallback.Type);

            return Expression.Call(Expression.Constant(this), typedMethod, target,
                Expression.Constant(property.ToString()), fallback);
        }

        public T GetProperty<T>(object instance, string property, T fallback)
        {
            if (instance == null)
            {
                return fallback;
            }

            var t = instance.GetType();

            if (!_typeMap.TryGetValue(t, out var mappings))
            {
                _typeMap[t] = mappings = new Dictionary<string, Func<object>>();
            }

            if (!mappings.TryGetValue(property, out var fact))
            {
                var prop = t.GetProperty(property);

                if (prop == null)
                {
                    return fallback;
                }

                mappings[property] = fact = () => prop.GetValue(instance);
            }

            return (T)fact();
        }
    }
}