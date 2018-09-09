using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class ExpressionParser
    {
        readonly IFunctionProvider _functionProvider;

        public ExpressionParser(IFunctionProvider functionProvider)
        {
            _functionProvider = functionProvider;
        }

        public LambdaExpression Parse(string expression, Type[] parameterTypes, Type outputType = null)
        {
            return Parse(expression, p => parameterTypes[p.Index], outputType);
        }

        public LambdaExpression Parse(string expression, Func<Parameter, Type> parameterBinder, Type outputType = null)
        {
            var (body, parameterExpressions) = ParseAndBindToExpression(expression, parameterBinder);
            
            var convertedBody = outputType == null ? body : body.Convert(outputType);

            return Expression.Lambda(convertedBody, parameterExpressions);
        }

        public (Expression body, Parameter[] parameters) Parse(string expression, Type outputType = null)
        {
            var paramTypes = new List<Parameter>();

            var (body, parameterExpressions) = ParseAndBindToExpression(expression, p =>
            {
                paramTypes.Add(p);
                return p.Type;
            });
            
            var convertedBody = outputType == null ? body : body.Convert(outputType);

            return (Expression.Lambda(convertedBody, parameterExpressions), paramTypes.ToArray());
        }

        public Expression<Func<TInput, TOutput>> Parse<TInput, TOutput>(string expression)
        {
            var (body, parameterExpressions) = ParseAndBindToExpression(expression, p => p.Index > 0 ? throw new ArgumentException() : typeof(TInput));

            return Expression.Lambda<Func<TInput, TOutput>>(body.Convert<TOutput>(), parameterExpressions);
        }

        (Expression body, ParameterExpression[] parameters) ParseAndBindToExpression(string expression, Func<Parameter, Type> parameterBinder)
        {
            var parts = GetExpressionParts(expression);
            var extr = new ExpressionTreeReader();
            var root = extr.Read(parts.body);
            var parameterTree = extr.Read(parts.paramNames);

            var parameters = Parameter.GetParameters(parameterTree)
                .Select((p, i) =>
                    Expression.Parameter(parameterBinder(p), p.Name))
                .ToArray();

            var body = Build(root, parameters);

            return (body, parameters);
        }

        Expression Build(ExpressionTree expressionTree, params ParameterExpression[] parameters)
        {
            var scope = new Scope(_functionProvider, parameters);

            return expressionTree.Build(scope).Single();
        }

        static (string paramNames, string body) GetExpressionParts(string expression)
        {
            var i = expression.IndexOf("=>", StringComparison.Ordinal);

            if (i == -1) throw new ArgumentException("Missing lamda");

            var body = expression.Substring(i + 2, expression.Length - (i + 2));
            var paramNames = expression.Substring(0, i).Trim();

            return (paramNames, body);
        }
    }
}