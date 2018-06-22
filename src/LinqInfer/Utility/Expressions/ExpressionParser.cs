using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    public class ExpressionParser<T>
    {
        public Expression<Func<T, double>> Parse(string expression)
        {
            var extr = new ExpressionTreeReader();
            var i = expression.IndexOf("=>", StringComparison.Ordinal);
            var root = extr.Read(expression.Substring(i + 2, expression.Length - (i + 2)));
            var paramName = expression.Substring(0, i).Trim();
            var parameter = Expression.Parameter(typeof(T), paramName);
            var body = Convert(parameter, root);
            return Expression.Lambda<Func<T, double>>(body, parameter);
        }

        internal Expression Convert(Expression context, ExpressionTree expressionTree)
        {
            return expressionTree.Convert(context).Single();
        }
    }
}