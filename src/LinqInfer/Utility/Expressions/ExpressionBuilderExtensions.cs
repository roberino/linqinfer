﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    static class ExpressionBuilderExtensions
    {
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
                    yield return expressionTree.AsSingleExpression(context);
                    break;
                case TokenType.Negation:
                    yield return Expression.Negate(expressionTree.AsSingleExpression(context));
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
                    Parameters = Parameter.GetParameters(expressionTree.Children.First()).ToArray()
                };

                yield break;
            }

            var items = Build(expressionTree, context).ToArray();

            if (!items.Any()) yield break;
            
            var item = items.AsSingleExpression();

            yield return new UnboundArgument(expressionTree, context, item);
        }

        static Expression AsSingleExpression(this ExpressionTree expressionTree, Scope context)
        {
            expressionTree.ValidateArgs(1, 1);

            return expressionTree.Children.Single().Build(context).Single();
        }

        static Expression AsArrayOrIndexer(this ExpressionTree expressionTree, Scope context)
        {
            var elements = expressionTree.Children.Select(c => c.Build(context).Single()).ToArray();

            if (expressionTree.Parent.IsIndexedAccessor)
            {
                return context.SelectIndexScope(ConversionFunctions.ConvertAll<int>(elements)).CurrentContext;
            }

            var converted = ConversionFunctions.MakeCompatible(elements);

            return Expression.NewArrayInit(converted.commonType, converted.expressions);
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

            return !expressionTree.Value.Contains('.') ? 
                Expression.Constant(int.Parse(expressionTree.Value), typeof(int)) : 
                Expression.Constant(double.Parse(expressionTree.Value), typeof(double));
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
                               expressionTree.AsGlobalNamedConstant(context);
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

            var (inputs, output) = InferredTypeResolver.GetFuncArgs(context.CurrentContext.Type);

            var converted = parameters.Zip(inputs).Select(p => p.a.Resolve().ConvertToType(p.b));

            var result = Expression.Invoke(context.CurrentContext, converted);

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

        static Expression AsGlobalNamedConstant(this ExpressionTree expression, Scope context)
        {
            return context.NameBinder.BindToName(expression.Value);
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

        static Expression AsSingleExpression(this IReadOnlyCollection<Expression> parts)
        {
            if (parts.Count == 0)
            {
                throw new ArgumentException();
            }

            if (parts.Count == 1)
            {
                return parts.Single();
            }

            return ConversionFunctions.ToTuple(parts);
        }

        static Expression AsLamdaExpression(this ExpressionTree expressionTree, Scope context)
        {
            var iscope = (InferredScope)context;

            var bodyParts = Build(expressionTree.Children.Last(), iscope).ToArray();

            var body = bodyParts.AsSingleExpression();

            if (iscope.OutputType != null)
            {
                iscope.TypeResolver.Infer(iscope.OutputType, body.Type);

                if (body.Type != iscope.OutputType)
                {
                    var resolvedType = iscope.TypeResolver.TryConstructType(iscope.OutputType);

                    if (body.Type != resolvedType)
                        body = Expression.Convert(body, iscope.OutputType);
                }
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

            var leaves = ConversionFunctions.MakeCompatible(first, right);
            
            return Expression.MakeBinary(expressionType, leaves.left, leaves.right);
        }
    }
}