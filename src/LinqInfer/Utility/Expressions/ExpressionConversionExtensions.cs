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
            var newContext = context.IsRoot ? context.SelectParameterScope(expressionTree.Value) : null;
            
            if (newContext == null)
            {
                if (expressionTree.IsMethod())
                {
                    return expressionTree.AsMethodCall(context);
                }

                if (context.IsRoot && expressionTree.IsMethodPath())
                {
                    return expressionTree.AsStaticMethodCall(context);
                }
                
                newContext = context.SelectChildScope(expressionTree.Value);
            }

            if (expressionTree.Children.Any())
            {
                return expressionTree.Children.Single().Build(newContext).Single();
            }

            return newContext.CurrentContext;
        }

        static Expression AsStaticMethodCall(this ExpressionTree expressionTree, Scope context)
        {
            var type = string.Empty;

            var next = expressionTree;

            while (!next.IsMethod())
            {
                type += next.Value;
                next = next.Children.Single();
            }

            type = type.TrimEnd('.');

            var args = next.Children.SelectMany(c => c.Build(context.GlobalContext));
            var binder = context.GetStaticBinder(type);
            return binder.GetFunction(next.Value, args);
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

        static bool IsMethodPath(this ExpressionTree expressionTree)
        {
            var child = expressionTree;

            while (child.Children.Count == 1)
            {
                if (child.IsMethod())
                {
                    return true;
                }

                child = child.Children.Single();
            }

            return false;
        }

        static Expression AsGlobalFunction(this ExpressionTree expression, Scope context)
        {
            if (!GlobalFunctions.IsDefined(expression.Value)) return null;

            var args = expression.Children.SelectMany(c => c.Build(context.GlobalContext)).ToArray();

            return GlobalFunctions.GetFunction(expression.Value, args);
        }

        static Expression AsTypeConstant(this ExpressionTree expression)
        {
            if (!Enum.TryParse(expression.Value, false, out TypeCode t)) return null;

            switch (t)
            {
                case TypeCode.Double:
                    return Expression.Constant(typeof(double));
                case TypeCode.Boolean:
                    return Expression.Constant(typeof(bool));
                case TypeCode.Int32:
                    return Expression.Constant(typeof(int));
                case TypeCode.String:
                    return Expression.Constant(typeof(string));
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
                    return Expression.Constant(false);
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