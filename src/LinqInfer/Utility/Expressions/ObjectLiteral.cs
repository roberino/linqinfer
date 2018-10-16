using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    public sealed class ObjectLiteral
    {
        readonly IDictionary<string, object> _properties;

        ObjectLiteral(IDictionary<string, object> properties)
        {
            _properties = properties;
        }

        public object Value(string name) => _properties[name];
        
        public static bool IsObjectLiteral(Expression expression)
        {
            return expression.Type == typeof(ObjectLiteral);
        }

        public static Expression Bind(Expression objectLiteralExpression, string name)
        {
            if (objectLiteralExpression is ConstantExpression)
            {
                return Bind((ConstantExpression) objectLiteralExpression, name);
            }

            if (objectLiteralExpression is InvocationExpression invokeExpression && invokeExpression.Arguments.Count == 0)
            {
                var lambda = (LambdaExpression)invokeExpression.Expression;

                return Bind((ConstantExpression) lambda.Body, name);
            }

            throw new ArgumentException(objectLiteralExpression.ToString());
        }

        static Expression Bind(ConstantExpression objectLiteralConstant, string name)
        {
            var literal = (ObjectLiteral) objectLiteralConstant.Value;

            var value = literal._properties[name];

            var type = value.GetType();

            return Expression.Constant(value, type);
        }

        public static ConstantExpression Create(IDictionary<string, object> properties)
        {
            return Expression.Constant(new ObjectLiteral(properties));
        }

        public static LambdaExpression CreateInvocatable(IDictionary<string, object> properties)
        {
            return Expression.Lambda(Expression.Constant(new ObjectLiteral(properties)));
        }
    }
}