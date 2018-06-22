using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    internal static class MathFunctions
    {
        static readonly IDictionary<string, MethodInfo[]> _mathMethods =
            typeof(Math)
                .GetTypeInf()
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .GroupBy(m => m.Name)
                .ToDictionary(g => g.Key, g => g.ToArray());

        public static bool IsDefined(string name)
        {
            return _mathMethods.ContainsKey(name);
        }

        public static Expression GetFunction(string name, IEnumerable<Expression> parameters)
        {
            var method = _mathMethods[name].First(m => IsParameterMatch(m.GetParameters(), parameters.Select(p => p.Type)));

            return Expression.Call(null, method, parameters);
        }

        static bool IsParameterMatch(IEnumerable<ParameterInfo> parameters, IEnumerable<Type> argTypes)
        {
            return new HashSet<Type>(parameters.Select(p => p.ParameterType)).SetEquals(argTypes);
        }
    }
}