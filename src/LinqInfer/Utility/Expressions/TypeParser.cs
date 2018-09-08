﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    static class TypeParser
    {
        static IDictionary<string, Type> _lookup = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["int"] = typeof(int),
            ["Int32"] = typeof(int),
            ["long"] = typeof(long),
            ["Int64"] = typeof(long),
            ["string"] = typeof(string),
            ["float"] = typeof(float),
            ["double"] = typeof(double),
            ["object"] = typeof(object)
        };

        public static Type AsType(this ExpressionTree expression, bool throwOnError = true)
        {
            if (_lookup.TryGetValue(expression.Value, out Type value))
            {
                return value;
            }

            if (expression.Value == "func")
            {
                var args = expression.Parameters.Select(p => p.AsType()).ToArray();

                var funcType = TypeExtensions.GetFuncType(args.Length - 1);

                return typeof(Expression<>).MakeGenericType(funcType.MakeGenericType(args));
            }

            if (throwOnError)
            {
                throw new NotSupportedException(expression.Value);
            }

            return null;
        }

        public static Expression AsTypeConstant(this ExpressionTree expression)
        {
            var type = AsType(expression, false);

            return type == null ? null : Expression.Constant(type);
        }
    }
}
