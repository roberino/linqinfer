using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
                        yield return expressionTree.AsOperatorExpression(context);
                        break;
                    }
                case TokenType.Condition:
                {
                    yield return expressionTree.AsCondition(context);
                    break;
                }
                case TokenType.Name:
                    {
                        if (context.IsRoot)
                        {
                            var glob = expressionTree.AsGlobalFunction(context) ?? 
                                expressionTree.AsTypeConstant() ?? 
                                expressionTree.AsGlobalNamedConstant();

                            if (glob != null)
                            {
                                yield return glob;
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
                    yield return expressionTree.AsLiteral(context);
                    break;
                case TokenType.Root:
                    yield return expressionTree.Children.Single().Convert(context).Single();
                    break;
                default:
                    throw new NotSupportedException(expressionTree.Type.ToString());
            }
        }

        static Expression AsCondition(this ExpressionTree expressionTree, Scope context)
        {
            var parts = expressionTree.Children.Select(c => c.Convert(context).Single()).ToArray();

            return Expression.Condition(parts[0], parts[1], parts[2]);
        }

        static Expression AsLiteral(this ExpressionTree expressionTree, Scope context)
        {
            if (context.ConversionType == null)
            {
                return Expression.Constant(double.Parse(expressionTree.Value), typeof(double));
            }

            var value = System.Convert.ChangeType(expressionTree.Value, context.ConversionType);
            return Expression.Constant(value, context.ConversionType);
        }

        static Expression AsMemberAccessor(this ExpressionTree expressionTree, Scope context)
        {
            var pe = context.CurrentContext as ParameterExpression;

            Scope newContext;

            if (pe != null)
            {
                if (context.IsRoot && pe.Name == expressionTree.Value)
                {
                    newContext = context.NewScope(pe);
                }
                else
                {
                    if (expressionTree.IsMethod())
                    {
                        return expressionTree.AsMethodCall(context);
                    }

                    newContext =
                        context.NewScope(Expression.PropertyOrField(context.CurrentContext, expressionTree.Value));
                }
            }
            else
            {
                if (expressionTree.IsMethod())
                {
                    return expressionTree.AsMethodCall(context);
                }
                
                newContext = context.NewScope(Expression.PropertyOrField(context.CurrentContext, expressionTree.Value));
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

        static Expression AsGlobalNamedConstant(this ExpressionTree expression)
        {
            switch (expression.Value)
            {
                case "true":
                    return Expression.Constant(true);
                case "false":
                    return Expression.Constant(true);
                default:
                    return null;
            }
        }

        static Expression AsOperatorExpression(this ExpressionTree expressionTree, Scope context)
        {
            Expression left, right;
            var first = expressionTree.Children.First().Convert(context).Single();

            if (!expressionTree.IsFull)
            {
                if (expressionTree.Value == "-")
                {
                    return Expression.Negate(first);
                }
                
                left = Expression.Constant(System.Convert.ChangeType(0, first.Type), first.Type);
                
                right = first;
            }
            else
            {
                left = first;
                right = expressionTree.Children.Last().Convert(context.NewConversionScope(left.Type))
                    .Single();
            }

            return expressionTree.CreateBinaryExpression(left, right);
        }

        static BinaryExpression CreateBinaryExpression(this ExpressionTree expression, Expression left,
            Expression right)
        {
            return Expression.MakeBinary(expression.Value.AsExpressionType(), left, right);
        }
    }
}