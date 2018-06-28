using System;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    internal static class ExpressionSerialiserExtensions
    {
        public static Expression<Func<TInput, TOutput>> AsExpression<TInput, TOutput>(
            this string expression)
        {
            return new ExpressionParser<TInput, TOutput>().Parse(expression);
        }

        public static Func<TOutput> AsFunc<TInput, TOutput>(
            this string expression,
            TInput input, 
            TOutput defaultValue)
        {
            var exp = new ExpressionParser<TInput, TOutput>().Parse(expression);
            var func = exp.Compile();
            
            return () => func(input);
        }

        public static string ExportAsString<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
        {
            return expression.ToString();
        }
    }
}