using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    internal static class ExpressionHelperExtensions
    {
        public static IEnumerable<Expression> Convert(this ExpressionTree expressionTree, Expression context)
        {
            switch (expressionTree.Type)
            {
                case TokenType.Operator:
                {
                    var left = expressionTree.Children.First().Convert(context).Single();
                    var right = expressionTree.Children.Last().Convert(context).Single();
                    yield return expressionTree.CreateBinaryExpression(left, right);
                    break;
                }
                case TokenType.Name:
                {
                    var globalFunc = expressionTree.AsGlobalFunction(context);

                    if (globalFunc != null)
                    {
                        yield return globalFunc;
                        break;
                    }

                    var asTypeConstant = expressionTree.AsTypeConstant();

                    if (asTypeConstant != null)
                    {
                        yield return asTypeConstant;
                        break;
                    }

                    yield return expressionTree.AsMemberAccessor(context);
                    break;
                }

                case TokenType.Navigate:
                case TokenType.GroupOpen:
                {
                    foreach (var child in expressionTree.Children.SelectMany(e => e.Convert(context)))
                    {
                        yield return child;
                    }

                    break;
                }
                case TokenType.Literal:
                    yield return Expression.Constant(double.Parse(expressionTree.Value), typeof(double));
                    break;
                default:
                    throw new NotSupportedException(expressionTree.Type.ToString());
            }
        }

        static Expression AsMemberAccessor(this ExpressionTree expressionTree, Expression context)
        {           
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

            if (expressionTree.Children.Any())
            {
                return expressionTree.Children.Single().Convert(newContext).Single();
            }

            return newContext;
        }

        static Expression AsGlobalFunction(this ExpressionTree expression, Expression context)
        {
            if (MathFunctions.IsDefined(expression.Value))
            {
                var args = expression.Children.SelectMany(c => c.Convert(context)).ToArray();

                return MathFunctions.GetFunction(expression.Value, args);
            }

            if (expression.Value == "Convert")
            {   
                var args = expression.Children.SelectMany(c => c.Convert(context)).ToArray();
                    
                return Expression.Convert(args.First(), (Type)((ConstantExpression)args.Last()).Value);
            }

            return null;
        }

        static Expression AsTypeConstant(this ExpressionTree expression)
        {
            if (!Enum.TryParse(expression.Value, false, out TypeCode t)) return null;

            switch (t)
            {
                case TypeCode.Double:
                    return Expression.Constant(typeof(double));
                default:
                    return null;
            }
        }

        static BinaryExpression CreateBinaryExpression(this ExpressionTree expression, Expression left,
            Expression right)
        {
            switch (expression.Value)
            {
                case "+":
                    return Expression.Add(left, right);
                case "*":
                    return Expression.Multiply(left, right);
                case "/":
                    return Expression.Divide(left, right);
                case "-":
                    return Expression.Subtract(left, right);
            }

            throw new NotSupportedException(expression.Value);
        }
    }
}