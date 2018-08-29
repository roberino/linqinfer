using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqInfer.Utility.Expressions
{
    class FunctionBinder
    {
        readonly IDictionary<string, MethodInfo[]> _methods;

        public FunctionBinder(Type type, BindingFlags bindingFlags)
        {
            _methods =
                type
                    .GetTypeInf()
                    .GetMethods(BindingFlags.Public | bindingFlags)
                    .GroupBy(m => m.Name)
                    .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public bool IsDefined(string name)
        {
            return _methods.ContainsKey(name);
        }

        public Expression GetFunction(string name, IEnumerable<Expression> parameters)
        {
            var method = _methods[name].FirstOrDefault(m => IsParameterMatch(m.GetParameters(), parameters.Select(p => p.Type)));

            if (method == null)
            {
                throw new ArgumentException();
            }

            return Expression.Call(null, method, Convert(parameters, method.GetParameters()));
        }

        public Expression GetFunction(Expression instance, string name, IEnumerable<Expression> parameters)
        {
            var method = _methods[name].FirstOrDefault(m => IsParameterMatch(m.GetParameters(), parameters.Select(p => p.Type)));
            
            if (method == null)
            {
                throw new ArgumentException();
            }

            return Expression.Call(instance, method, Convert(parameters, method.GetParameters()));
        }

        static bool IsParameterMatch(IEnumerable<ParameterInfo> parameters, IEnumerable<Type> argTypes)
        {
            return new HashSet<Type>(parameters.Select(p => p.ParameterType), TypeEqualityComparer.Instance).SetEquals(argTypes);
        }

        static IEnumerable<Expression> Convert(IEnumerable<Expression> expressions, IEnumerable<ParameterInfo> parameters)
        {
            foreach (var item in expressions.Zip(parameters.Select(p => p.ParameterType), (e, t) => (e, t)))
            {
                if (TypeEqualityComparer.Instance.RequiresConversion(item.e.Type, item.t).GetValueOrDefault())
                {
                    if (item.e.NodeType == ExpressionType.Constant && item.e is ConstantExpression ce)
                    {
                        yield return Expression.Constant(System.Convert.ChangeType(ce.Value, item.t));
                        continue;
                    }

                    yield return Expression.Convert(item.e, item.t);
                    continue;
                }

                yield return item.e;
            }
        }

        IEnumerable<MethodInfo> GetExtensionMethods(Type extendedType)
        {
            return 
                from method in _methods.SelectMany(m => m.Value)
                where method.IsDefined(typeof(ExtensionAttribute), false)
                where method.GetParameters()[0].ParameterType.IsAssignableFrom(extendedType)
                select method;
        }
    }
}