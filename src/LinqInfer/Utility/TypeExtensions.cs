using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqInfer.Utility
{
    public static class TypeExtensions
    {
        private static readonly Type nullableType = typeof(Nullable<>);

#if NET_STD
        public static TypeInfo GetTypeInf<T>()
        {
            return typeof(T).GetTypeInfo();
        }

        public static TypeInfo GetTypeInf(this Type type)
        {
            return type.GetTypeInfo();
        }

        public static MethodInfo GetMethodInf(this Delegate func)
        {
            return func.GetMethodInfo();
        }
#else
        public static Type GetTypeInf<T>()
        {
            return typeof(T);
        }

        public static Type GetTypeInf(this Type type)
        {
            return type;
        }

        public static MethodInfo GetMethodInf(this Delegate func)
        {
            return func.Method;
        }
#endif

        public static bool IsAnonymous<T>()
        {
            var type = GetTypeInf<T>();
            return type.GetCustomAttributes(false).Any(a => a.GetType() == typeof(CompilerGeneratedAttribute))
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static Type GetNullableTypeType(this Type type)
        {
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == nullableType)
            {
                return type.GetTypeInf().GetGenericArguments()[0];
            }
            return null;
        }

        public static object MakeNullableType(this Type innerType, object value)
        {
            return nullableType
                .MakeGenericType(innerType)
                .GetTypeInfo()
                .GetConstructor(new Type[] { innerType })
                .Invoke(new object[] { value });
        }
    }
}