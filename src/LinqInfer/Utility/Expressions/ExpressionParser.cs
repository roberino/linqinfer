using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    public class ExpressionParser<TInput, TOutput>
    {
        public Expression<Func<TInput, TOutput>> Parse(string expression)
        {
            var extr = new ExpressionTreeReader();
            var i = expression.IndexOf("=>", StringComparison.Ordinal);
            var root = extr.Read(expression.Substring(i + 2, expression.Length - (i + 2)));
            var paramName = expression.Substring(0, i).Trim();
            var parameter = Expression.Parameter(typeof(TInput), paramName);
            var body = Convert(parameter, root);
            return Expression.Lambda<Func<TInput, TOutput>>(body, parameter);
        }

        internal Expression Convert(Expression context, ExpressionTree expressionTree)
        {
            return expressionTree.Convert(new Scope(context)).Single();
        }
    }
}