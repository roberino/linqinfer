﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    internal static class ExpressionSerialiserExtensions
    {
        public static Expression<Func<TInput, TOutput>> AsExpression<TInput, TOutput>(
            this string expression)
        {
            return new ExpressionParser<TInput, TOutput>().Parse(expression);
        }

        public static Func<TOutput> AsFunc<TInput, TOutput>(
            this string expression,
            TInput input,
            TOutput defaultValue)
        {
            var exp = new ExpressionParser<TInput, TOutput>().Parse(expression);
            var func = exp.Compile();

            return () => func(input);
        }

        public static string ExportAsString<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
        {
            return ExportExpression(expression);
        }

        private static string ExportExpression(this Expression expression)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                return ExportBinaryExpression(binaryExpression, binaryExpression.NodeType.AsString());
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    var callExp = (MethodCallExpression)expression;
                    var args = callExp.Arguments.Select(a => a.ExportExpression());
                    var argss = string.Join(", ", args);
                    var obj = string.Empty;

                    if (callExp.Object != null)
                    {
                        obj = callExp.Object.ExportExpression() + ".";
                    }
                    return $"{obj}{callExp.Method.Name}({argss})";
                case ExpressionType.Constant:
                    var constExp = (ConstantExpression)expression;
                    return constExp.Value.ToString();
                case ExpressionType.Lambda:
                    var lam = ((LambdaExpression)expression);
                    var parms = lam.Parameters.Select(p => p.ExportExpression());
                    var paramss = string.Join(", ", parms);
                    return $"{paramss} => {lam.Body.ExportExpression()}";
                case ExpressionType.Parameter:
                    return ((ParameterExpression)expression).Name;
                case ExpressionType.MemberAccess:

                    var memExp = (MemberExpression)expression;

                    if (memExp.Expression is ConstantExpression constantExpression)
                    {
                        return ExportConstantValue(constantExpression, memExp.Member);
                    }

                    var path = memExp.Expression.ExportExpression();

                    return $"{path}.{memExp.Member.Name}";
                case ExpressionType.Convert:
                    {
                        var unex = (UnaryExpression)expression;
                        return $"Convert(({unex.Operand.ExportExpression()}), {expression.Type.Name})";
                    }
                case ExpressionType.Negate:
                    {
                        var unex = (UnaryExpression)expression;
                        var exp = ExportWithBracketsIfRequired(unex.Operand);

                        return $"-{exp}";
                    }
                case ExpressionType.Conditional:
                    var ce = (ConditionalExpression) expression;

                    var test = ce.Test.ExportExpression();
                    var iftrue = ExportWithBracketsIfRequired(ce.IfTrue);
                    var iffalse = ExportWithBracketsIfRequired(ce.IfFalse);

                    return $"({test} ? {iftrue} : {iffalse})";

                default:
                    throw new NotSupportedException(expression.NodeType.ToString());
            }
        }

        private static string ExportWithBracketsIfRequired(Expression exp)
        {
            var exps = exp.ExportExpression();

            return exp is ConstantExpression ? $"{exps}" : $"({exps})";
        }

        private static string ExportConstantValue(ConstantExpression constant, MemberInfo member)
        {
            switch (member)
            {
                case PropertyInfo prop:
                    return prop.GetValue(constant.Value)?.ToString();
                case FieldInfo field:
                    return field.GetValue(constant.Value)?.ToString();
            }

            throw new NotSupportedException(member.GetType().Name);
        }

        private static string ExportBinaryExpression(BinaryExpression expression, string symbol)
        {
            return $"({ExportExpression(expression.Left)} {symbol} {ExportExpression(expression.Right)})";
        }
    }
}