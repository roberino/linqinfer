using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    static class ExpressionBuilderExtensions
    {
        public static Expression Convert<T>(this Expression expression)
        {
            return expression.Convert(typeof(T));
        }

        public static Expression Convert(this Expression expression, Type type)
        {
            return expression.Type != type ?
                Expression.Convert(expression, type) :
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
                        yield return expressionTree.AsNamedExpression(context);
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
                    yield return expressionTree.AsArrayOrIndexer(context);
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

        static UnboundArgument[] BuildParameters(this ExpressionTree expressionTree, Scope context)
        {
            return expressionTree.Parameters.SelectMany(c => c.BuildParameter(context.RootScope)).ToArray();
        }

        static IEnumerable<UnboundArgument> BuildParameter(this ExpressionTree expressionTree, Scope context)
        {
            if (expressionTree.IsLamda)
            {
                yield return new UnboundArgument(expressionTree, context)
                {
                    Resolver = (t, c) => Build(t, c).Single(),
                    ParameterNames = expressionTree.Children.First().Names.ToArray()
                };

                yield break;
            }

            foreach (var item in Build(expressionTree, context))
            {
                yield return new UnboundArgument(expressionTree, context, item);
            }
        }

        static Expression SingleParameter(this ExpressionTree expressionTree, Scope context)
        {
            expressionTree.ValidateArgs(1, 1);

            return expressionTree.Children.Single().Build(context).Single();
        }

        static Expression AsArrayOrIndexer(this ExpressionTree expressionTree, Scope context)
        {
            var elements = expressionTree.Children.Select(c => c.Build(context).Single()).ToArray();

            if (expressionTree.Parent.IsIndexedAccessor)
            {
                return context.SelectIndexScope(elements).CurrentContext;
            }

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

        static Expression AsNamedExpression(this ExpressionTree expressionTree, Scope context)
        {
            if (context.IsRoot)
            {
                Expression glob = null;

                if (expressionTree.IsFunction)
                {
                    glob = expressionTree.AsGlobalFunction(context);
                }
                else
                {
                    if (expressionTree.Children.Count == 0)
                    {
                        glob = expressionTree.AsTypeConstant() ??
                               expressionTree.AsGlobalNamedConstant();
                    }
                }

                if (glob != null)
                {
                    return glob;
                }
            }

            return expressionTree.AsMemberAccessor(context);
        }

        static Expression AsMemberAccessor(this ExpressionTree expressionTree, Scope context)
        {
            var newContext = context.IsRoot ? context.SelectParameterScope(expressionTree.Value) : null;

            if (newContext == null)
            {
                if (context.CurrentContext == null)
                {
                    if (context.IsRoot && expressionTree.IsMethodPath())
                    {
                        return expressionTree.AsStaticMethodCall(context);
                    }

                    return Expression.Constant(context.TokenCache.Get(expressionTree.Value));
                }

                if (expressionTree.IsMethod())
                {
                    return expressionTree.AsMethodCall(context);
                }

                newContext = context.SelectChildScope(expressionTree.Value);
            }
            else
            {
                if (expressionTree.IsLambdaCall(newContext))
                {
                    return expressionTree.InvokeLambda(newContext);
                }
            }

            return FollowPath(expressionTree.Children, newContext);
        }

        static bool IsLambdaCall(this ExpressionTree expressionTree, Scope context)
        {
            return expressionTree.IsMethod() &&
                   (context.CurrentContext.Type.IsSubclassOf(typeof(Expression))
                    || context.CurrentContext.Type.IsFunc());
        }

        static Expression InvokeLambda(this ExpressionTree expressionTree, Scope context)
        {
            var parameters = expressionTree.BuildParameters(context.RootScope);

            var result = Expression.Invoke(context.CurrentContext, parameters.Select(p => p.Resolve()));

            var nextScope = context.SelectChildScope(result);
                
            if (expressionTree.Children.Count <= 1) return nextScope.CurrentContext;

            return FollowPath(expressionTree.Children.Skip(1), nextScope);
        }

        static Expression FollowPath(this IEnumerable<ExpressionTree> paths, Scope context)
        {
            var nextScope = context;

            foreach (var path in paths)
            {
                if (path.Type == TokenType.ArrayOpen)
                {
                    nextScope = nextScope.SelectIndexScope(path.BuildParameters(context.RootScope)
                        .Select(p => p.Resolve()));
                    continue;
                }

                if (path.Type == TokenType.Name)
                {
                    nextScope = nextScope.SelectChildScope(path.AsMemberAccessor(nextScope));

                    continue;
                }

                if (path.Type == TokenType.Navigate)
                {
                    return FollowPath(path.Children, nextScope);
                }

                throw new CompileException(path.Value, path.Position, CompileErrorReason.UnknownToken);
            }

            return nextScope.CurrentContext;
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

            var args = next.BuildParameters(context.RootScope);

            try
            {
                var binder = context.Functions.GetStaticBinder(type);

                var result = binder.BindToFunction(next.Value, args);

                var nextScope = context.SelectChildScope(result);
                
                if (next.Children.Count <= 1) return nextScope.CurrentContext;

                return FollowPath(next.Children.Skip(1), nextScope);
            }
            catch (ArgumentException)
            {
                throw new CompileException(expressionTree.Value, expressionTree.Position,
                    CompileErrorReason.InvalidArgs);
            }
            catch (MemberAccessException)
            {
                throw new CompileException(type, expressionTree.Position, CompileErrorReason.UnknownFunction);
            }
        }

        static Expression AsMethodCall(this ExpressionTree expressionTree, Scope context)
        {
            var args = expressionTree.Parameters.SelectMany(c => c.BuildParameter(context.RootScope)).ToArray();
            var binder = context.GetBinder();

            try
            {
                var result = binder.BindToFunction(expressionTree.Value, args, context.CurrentContext);

                var nextScope = context.SelectChildScope(result);
                
                if (expressionTree.Children.Count <= 1) return nextScope.CurrentContext;

                return FollowPath(expressionTree.Children.Skip(1), nextScope);
            }
            catch (ArgumentException)
            {
                throw new CompileException(expressionTree.Value, expressionTree.Position,
                    CompileErrorReason.InvalidArgs);
            }
            catch (KeyNotFoundException)
            {
                throw new CompileException(expressionTree.Value, expressionTree.Position,
                    CompileErrorReason.UnknownFunction);
            }
        }

        static bool IsMethod(this ExpressionTree expressionTree)
        {
            return (expressionTree.Type == TokenType.Name &&
                    expressionTree.Children.Count > 0 &&
                    expressionTree.Children.FirstOrDefault()?.Type == TokenType.GroupOpen);
        }

        static bool IsMethodPath(this ExpressionTree expressionTree)
        {
            var child = expressionTree;

            while (child.Children.Count >= 1)
            {
                if (child.IsMethod())
                {
                    return true;
                }

                child = child.Children.First();
            }

            return false;
        }

        static Expression AsGlobalFunction(this ExpressionTree expressionTree, Scope context)
        {
            var gfb = context.Functions.GetGlobalBinder();

            if (!gfb.IsDefined(expressionTree.Value)) return null;

            var args = expressionTree.BuildParameters(context.RootScope);

            try
            {
                var result = gfb.BindToFunction(expressionTree.Value, args);

                var nextScope = context.SelectChildScope(result);
                
                if (expressionTree.Children.Count <= 1) return nextScope.CurrentContext;

                return FollowPath(expressionTree.Children.Skip(1), nextScope);
            }
            catch (ArgumentException ex)
            {
                Trace.Write(ex);
                throw new CompileException(expressionTree.Value, expressionTree.Position, CompileErrorReason.InvalidArgs);
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

        static Expression AsLamdaExpression(this ExpressionTree expressionTree, Scope context)
        {
            var iscope = (InferredScope)context;

            var body = Build(expressionTree.Children.Last(), iscope).Single();

            if (body.Type != iscope.OutputType && !iscope.OutputType.IsGenericParameter)
            {
                body = Expression.Convert(body, iscope.OutputType);
            }

            return Expression.Lambda(body, iscope.Parameters);
        }

        static Expression AsOperatorExpression(this ExpressionTree expressionTree, Scope context)
        {
            expressionTree.ValidateArgs(1, 2);

            var expressionType = expressionTree.Value.AsExpressionType();

            if (expressionType == ExpressionType.Lambda)
            {
                return expressionTree.AsLamdaExpression(context);
            }

            var first = expressionTree.Children.First().Build(context).Single();

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

            if (right.Type != first.Type)
            {
                right = Expression.Convert(right, first.Type);
            }

            return Expression.MakeBinary(expressionType, first, right);
        }
    }
}