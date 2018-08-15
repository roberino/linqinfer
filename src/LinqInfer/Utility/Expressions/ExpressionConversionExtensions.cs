using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    static class ExpressionConversionExtensions
    {
        public static Expression Convert<T>(this Expression expression)
        {
            return expression.Type != typeof(T) ? 
                Expression.Convert(expression, typeof(T)) : 
                expression;
        }

        public static IEnumerable<Expression> Build(this ExpressionTree expressionTree, Scope context)
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
                case TokenType.Separator:
                    {
                        foreach (var child in expressionTree.Children.SelectMany(e => e.Build(context)))
                        {
                            yield return child;
                        }

                        break;
                    }
                case TokenType.ArrayOpen:
                    yield return expressionTree.AsArray(context);
                    break;
                case TokenType.Literal:
                    yield return expressionTree.AsLiteral(context);
                    break;
                case TokenType.Root:
                    yield return expressionTree.SingleParameter(context);
                    break;
                case TokenType.Negation:
                    yield return Expression.Negate(expressionTree.SingleParameter(context));
                    break;
                default:
                    throw new NotSupportedException(expressionTree.Type.ToString());
            }
        }

        static Expression SingleParameter(this ExpressionTree expressionTree, Scope context)
        {
            expressionTree.ValidateArgs(1, 1);

            return expressionTree.Children.Single().Build(context).Single();
        }

        static Expression AsArray(this ExpressionTree expressionTree, Scope context)
        {
            var elements = expressionTree.Children.Select(c => c.Build(context).Single()).ToArray();

            var types = elements.Select(e => e.Type).Distinct().ToList();

            var type = types.Count == 1 ? types[0] : typeof(object);

            return Expression.NewArrayInit(type, elements);
        }

        static Expression AsCondition(this ExpressionTree expressionTree, Scope context)
        {
            expressionTree.ValidateArgs(3, 3);

            var parts = expressionTree.Children.Select(c => c.Build(context).Single()).ToArray();

            return Expression.Condition(parts[0], parts[1], parts[2]);
        }

        static Expression AsLiteral(this ExpressionTree expressionTree, Scope context)
        {
            expressionTree.ValidateArgs(0, 0);

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
                return expressionTree.Children.Single().Build(newContext).Single();
            }

            return newContext.CurrentContext;
        }

        static Expression AsMethodCall(this ExpressionTree expressionTree, Scope context)
        {
            var args = expressionTree.Children.SelectMany(c => c.Build(context.GlobalContext));
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
                var args = expression.Children.SelectMany(c => c.Build(context.GlobalContext)).ToArray();

                return MathFunctions.GetFunction(expression.Value, args);
            }

            if (expression.Value == "Convert")
            {
                var args = expression.Children.SelectMany(c => c.Build(context.GlobalContext)).ToArray();

                expression.ValidateArgs(args, 2, 2);

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

        static Expression AsNotExpression(this ExpressionTree expressionTree, Expression arg)
        {
            expressionTree.ValidateArgs(1, 1);

            return Expression.Not(arg);
        }

        static void ValidateArgs(this ExpressionTree expressionTree, int min, int max)
        {
            ValidateArgs(expressionTree, expressionTree.Children, min, max);
        }

        static void ValidateArgs<T>(this ExpressionTree expressionTree, IReadOnlyCollection<T> args, int min, int max)
        {
            if (args.Count > max)
            {
                throw new CompileException(expressionTree.Value, expressionTree.Position,
                    CompileErrorReason.TooManyArgs);
            }

            if (args.Count < min)
            {
                throw new CompileException(expressionTree.Value, expressionTree.Position,
                    CompileErrorReason.NotEnoughArgs);
            }
        }

        static Expression AsOperatorExpression(this ExpressionTree expressionTree, Scope context)
        {
            expressionTree.ValidateArgs(1, 2);

            var first = expressionTree.Children.First().Build(context).Single();
            var expressionType = expressionTree.Value.AsExpressionType();

            if (expressionType == ExpressionType.Not)
            {
                return AsNotExpression(expressionTree, first);
            }

            if (!expressionTree.IsFull)
            {
                if (expressionType == ExpressionType.Subtract)
                {
                    return Expression.Negate(first);
                }

                throw new CompileException(expressionTree.Value, expressionTree.Position,
                    CompileErrorReason.NotEnoughArgs);
            }

            var right = expressionTree.Children.Last()
                .Build(context.NewConversionScope(first.Type))
                .Single();

            return Expression.MakeBinary(expressionType, first, right);
        }
    }
}