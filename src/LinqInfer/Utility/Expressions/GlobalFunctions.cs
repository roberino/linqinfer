using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    internal static class MathFunctions
    {
        static readonly FunctionBinder _mathFunctions = new FunctionBinder(typeof(Math), BindingFlags.Static);

        public static bool IsDefined(string name)
        {
            return _mathFunctions.IsDefined(name);
        }

        public static Expression GetFunction(string name, IEnumerable<Expression> parameters)
        {
            return _mathFunctions.GetFunction(name, parameters);
        }
    }
}