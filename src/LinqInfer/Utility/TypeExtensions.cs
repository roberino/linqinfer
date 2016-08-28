using System;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Utility
{
    public static class TypeExtensions
    {
        private static readonly Type nullableType = typeof(Nullable<>);
        
        public static Type GetNullableTypeType(this Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == nullableType)
            {
                return type.GetGenericArguments()[0];
            }
            return null;
        }

        public static object MakeNullableType(this Type innerType, object value)
        {
            return nullableType
                .MakeGenericType(innerType)
                .GetConstructor(new Type[] { innerType })
                .Invoke(new object[] { value });
        }
    }
}