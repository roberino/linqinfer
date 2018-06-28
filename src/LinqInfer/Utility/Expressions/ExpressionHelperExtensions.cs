using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    internal static class ExpressionHelperExtensions
    {
        public static IEnumerable<Expression> Convert(this ExpressionTree expressionTree, Scope context)
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
                        if (context.IsRoot)
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

        static Expression AsMemberAccessor(this ExpressionTree expressionTree, Scope context)
        {
            var pe = context.CurrentContext as ParameterExpression;

            Scope newContext;

            if (pe != null)
            {
                if (pe.Name == expressionTree.Value)
                {
                    newContext = context.NewScope(pe);
                }
                else
                {
                    if (expressionTree.IsMethod())
                    {
                        return expressionTree.AsMethodCall(context);
                    }

                    newContext = context.NewScope(Expression.Property(context.CurrentContext, pe.Type, expressionTree.Value));
                }
            }
            else
            {
                if (expressionTree.IsMethod())
                {
                    return expressionTree.AsMethodCall(context);
                }

                newContext = context.NewScope(Expression.Property(context.CurrentContext, context.CurrentContext.Type, expressionTree.Value));
            }

            if (expressionTree.Children.Any())
            {
                return expressionTree.Children.Single().Convert(newContext).Single();
            }

            return newContext.CurrentContext;
        }

        static Expression AsMethodCall(this ExpressionTree expressionTree, Scope context)
        {
            var args = expressionTree.Children.SelectMany(c => c.Convert(context.GlobalContext));
            var binder = context.GetBinder();
            return binder.GetFunction(context.CurrentContext, expressionTree.Value, args);
        }

        static bool IsMethod(this ExpressionTree expressionTree)
        {
            return (expressionTree.Type == TokenType.Name &&
                    expressionTree.Children.SingleOrNull()?.Type == TokenType.GroupOpen);
        }

        static Expression AsGlobalFunction(this ExpressionTree expression, Scope context)
        {
            if (MathFunctions.IsDefined(expression.Value))
            {
                var args = expression.Children.SelectMany(c => c.Convert(context.GlobalContext)).ToArray();

                return MathFunctions.GetFunction(expression.Value, args);
            }

            if (expression.Value == "Convert")
            {
                var args = expression.Children.SelectMany(c => c.Convert(context.GlobalContext)).ToArray();

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