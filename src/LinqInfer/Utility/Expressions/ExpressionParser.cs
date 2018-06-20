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
            switch (expressionTree.Type)
            {
                case TokenType.Operator:
                    var left = Convert(context, expressionTree.Children.First());
                    var right = Convert(context, expressionTree.Children.Last());
                    return expressionTree.CreateBinaryExpression(left, right);
                case TokenType.Name:
                    var pe = context as ParameterExpression;

                    Expression newContext;

                    if (pe != null)
                    {
                        if (pe.Name == expressionTree.Value)
                        {
                            newContext = pe;
                        }
                        else
                        {
                            newContext = Expression.Property(context, pe.Type, expressionTree.Value);
                        }
                    }
                    else
                    {
                        var me = (MemberExpression) context;
                        var type = ((PropertyInfo) me.Member).PropertyType;
                        newContext = Expression.Property(context, type, expressionTree.Value);
                    }

                    if (expressionTree.Children.Any()) return Convert(newContext, expressionTree.Children.Single());

                    return newContext;

                case TokenType.Navigate:
                case TokenType.GroupOpen:
                    return Convert(context, expressionTree.Children.Single());
                case TokenType.Literal:
                    return Expression.Constant(double.Parse(expressionTree.Value), typeof(double));
                default:
                    throw new NotSupportedException(expressionTree.Type.ToString());
            }
        }
    }
}