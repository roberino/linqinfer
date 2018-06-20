using System;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    internal static class ExpressionHelperExtensions
    {
        public static BinaryExpression CreateBinaryExpression(this ExpressionTree expression, Expression left,
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