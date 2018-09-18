using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    static class ExpressionFormatter
    {
        public static string ExportExpression(this Expression expression)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                if (binaryExpression.NodeType == ExpressionType.ArrayIndex)
                {
                    return $"{binaryExpression.Left.ExportExpression()}[{binaryExpression.Right.ExportExpression()}]";
                }

                return ExportBinaryExpression(binaryExpression, binaryExpression.NodeType.AsString());
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    var callExp = (MethodCallExpression)expression;

                    return ExportCall(callExp);

                case ExpressionType.Constant:
                    var constExp = (ConstantExpression)expression;
                    return constExp.Value.ToString();
                case ExpressionType.Lambda:
                    var lam = ((LambdaExpression)expression);
                    var parms = lam.Parameters.Select(p => p.ExportExpression());
                    var paramss = string.Join(", ", parms);
                    return $"{paramss} => {lam.Body.ExportExpression()}";
                case ExpressionType.Quote:
                    var uni = (UnaryExpression) expression;
                    return uni.Operand.ExportExpression();
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
                        return $"Convert(({unex.Operand.ExportExpression()}), {expression.Type.Name.ToLower()})";
                    }
                case ExpressionType.Negate:
                    {
                        var unex = (UnaryExpression)expression;
                        var exp = ExportWithBracketsIfRequired(unex.Operand);

                        return $"-{exp}";
                    }
                case ExpressionType.Conditional:
                    var ce = (ConditionalExpression)expression;

                    var test = ce.Test.ExportExpression();
                    var iftrue = ExportWithBracketsIfRequired(ce.IfTrue);
                    var iffalse = ExportWithBracketsIfRequired(ce.IfFalse);

                    return $"({test} ? {iftrue} : {iffalse})";
                case ExpressionType.NewArrayInit:
                    {
                        var arrayExp = (NewArrayExpression)expression;
                        var elems = arrayExp.Expressions.Select(a => a.ExportExpression());
                        var elemsstr = string.Join(", ", elems);

                        return $"[{elemsstr}]";
                    }
                case ExpressionType.New:
                    return CreateNew((NewExpression)expression);
                default:
                    throw new NotSupportedException(expression.NodeType.ToString());
            }
        }

        static bool IsSingleArg<T>(this IReadOnlyCollection<Expression> args)
        {
            return args.Count == 1 && args.Single().Type == typeof(T);
        }

        static string CreateNew(NewExpression newExp)
        {
            if (newExp.Constructor.DeclaringType == typeof(Vector) || newExp.Constructor.DeclaringType == typeof(ColumnVector1D))
            {
                if (newExp.Arguments.IsSingleArg<double[]>())
                {
                    var args = newExp.Arguments.Select(a => a.ExportExpression()).Single();

                    var exp = $"{nameof(Vector)}({args})";

                    if (newExp.Constructor.DeclaringType == typeof(ColumnVector1D))
                    {
                        exp += $".{nameof(Vector.ToColumnVector)}()";
                    }

                    return exp;
                }
            }

            if (newExp.Constructor.DeclaringType == typeof(BitVector))
            {
                if (newExp.Arguments.IsSingleArg<bool[]>())
                {
                    var args = newExp.Arguments.Select(a => a.ExportExpression()).Single();

                    return $"{nameof(BitVector)}({args})";
                }
            }

            if (newExp.Constructor.DeclaringType == typeof(OneOfNVector))
            {
                var args = string.Join(",", newExp.Arguments.Select(a => a.ExportExpression()));

                return $"{nameof(OneOfNVector)}({args})";
            }

            if (newExp.Constructor.DeclaringType == typeof(Matrix))
            {
                if (newExp.Arguments.IsSingleArg<double[][]>())
                {
                    var args = newExp.Arguments.Select(a => a.ExportExpression()).Single();

                    return $"{nameof(Matrix)}({args})";
                }
            }

            throw new NotSupportedException(newExp.Constructor.DeclaringType?.Name);
        }

        static string ExportCall(MethodCallExpression callExp)
        {
            var args = callExp.Arguments.Select(a => a.ExportExpression());
            var argss = string.Join(", ", args);

            var obj = string.Empty;

            if (callExp.Object != null)
            {
                obj = callExp.Object.ExportExpression();

                if (callExp.Method.IsSpecialName && callExp.Method.Name == "get_Item")
                {
                    return $"{obj}[{argss}]";
                }
            }
            else
            {
                if (callExp.Method.DeclaringType != typeof(Math) && callExp.Method.DeclaringType != null)
                {
                    obj = callExp.Method.DeclaringType.Name;
                }
            }

            if (obj.Length > 0) obj += ".";

            return $"{obj}{callExp.Method.Name}({argss})";
        }

        static string ExportWithBracketsIfRequired(Expression exp)
        {
            var exps = exp.ExportExpression();

            return exp is ConstantExpression ? $"{exps}" : $"({exps})";
        }

        static string ExportConstantValue(ConstantExpression constant, MemberInfo member)
        {
            switch (member)
            {
                case PropertyInfo prop:
                    return prop.GetValue(constant.Value).ToPreciseString();
                case FieldInfo field:
                    return field.GetValue(constant.Value).ToPreciseString();
            }

            throw new NotSupportedException(member.GetType().Name);
        }

        static string ToPreciseString(this object value)
        {
            if (value == null)
            {
                return null;
            }

            var type = value.GetType();
            var tc = Type.GetTypeCode(type);

            switch (tc)
            {
                case TypeCode.Double:
                case TypeCode.Single:
                    return ((double)value).ToString("R");
                case TypeCode.String:
                    throw new NotSupportedException($"Unsupported constant: {type.Name}");
                case TypeCode.Object:
                    if (!(value is IVector vect)) throw new NotSupportedException($"Unsupported constant: {type.Name}");

                    switch (vect)
                    {
                        case OneOfNVector oneOfN:
                            if (oneOfN.ActiveIndex.HasValue)
                            {
                                return $"{nameof(OneOfNVector)}({oneOfN.Size}, {oneOfN.ActiveIndex})";
                            }

                            return $"{nameof(OneOfNVector)}({oneOfN.Size})";
                        case BitVector bitVect:
                            return $"{nameof(BitVector)}({string.Join(",", bitVect.Select(b => b.ToString()))})";
                    }

                    return $"{nameof(Vector)}({vect.ToColumnVector().ToCsv(int.MaxValue)})";
            }

            return value.ToString().ToLower();
        }

        static string ExportBinaryExpression(BinaryExpression expression, string symbol)
        {
            return $"({ExportExpression(expression.Left)} {symbol} {ExportExpression(expression.Right)})";
        }
    }
}
