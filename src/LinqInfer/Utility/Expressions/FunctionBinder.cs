using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
            var method = _methods[name].First(m => IsParameterMatch(m.GetParameters(), parameters.Select(p => p.Type)));

            return Expression.Call(null, method, parameters);
        }

        public Expression GetFunction(Expression instance, string name, IEnumerable<Expression> parameters)
        {
            var method = _methods[name].First(m => IsParameterMatch(m.GetParameters(), parameters.Select(p => p.Type)));

            return Expression.Call(instance, method, parameters);
        }

        static bool IsParameterMatch(IEnumerable<ParameterInfo> parameters, IEnumerable<Type> argTypes)
        {
            return new HashSet<Type>(parameters.Select(p => p.ParameterType)).SetEquals(argTypes);
        }
    }
}