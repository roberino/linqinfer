using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class ExpressionParser<TInput, TOutput>
    {
        readonly IFunctionProvider _functionProvider;

        public ExpressionParser(IFunctionProvider functionProvider)
        {
            _functionProvider = functionProvider;
        }

        public Expression<Func<TInput, TOutput>> Parse(string expression)
        {
            var parts = GetExpressionParts(expression);
            var extr = new ExpressionTreeReader();
            var root = extr.Read(parts.body);
            var parameter = Expression.Parameter(typeof(TInput), parts.paramName);
            var body = Build(parameter, root).Convert<TOutput>();

            return Expression.Lambda<Func<TInput, TOutput>>(body, parameter);
        }

        internal Expression Build(ParameterExpression context, ExpressionTree expressionTree)
        {
            var scope = new Scope(_functionProvider, context);

            return expressionTree.Build(scope).Single();
        }

        static (string paramName, string body) GetExpressionParts(string expression)
        {
            var i = expression.IndexOf("=>", StringComparison.Ordinal);

            if (i == -1) throw new ArgumentException("Missing lamda");

            var body = expression.Substring(i + 2, expression.Length - (i + 2));
            var paramName = expression.Substring(0, i).Trim();

            return (paramName, body);
        }
    }
}