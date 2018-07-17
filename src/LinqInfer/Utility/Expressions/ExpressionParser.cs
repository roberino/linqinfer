using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    public class ExpressionParser<TInput, TOutput>
    {
        public Expression<Func<TInput, TOutput>> Parse(string expression)
        {
            var parts = GetExpressionParts(expression);
            var extr = new ExpressionTreeReader();
            var root = extr.Read(parts.body);
            var parameter = Expression.Parameter(typeof(TInput), parts.paramName);
            var body = Convert(parameter, root);
            return Expression.Lambda<Func<TInput, TOutput>>(body, parameter);
        }

        internal Expression Convert(Expression context, ExpressionTree expressionTree)
        {
            return expressionTree.Convert(new Scope(context)).Single();
        }

        private static (string paramName, string body) GetExpressionParts(string expression)
        {
            var i = expression.IndexOf("=>", StringComparison.Ordinal);

            if (i == -1) throw new ArgumentException("Missing lamda");

            var body = expression.Substring(i + 2, expression.Length - (i + 2));
            var paramName = expression.Substring(0, i).Trim();

            return (paramName, body);
        }
    }
}