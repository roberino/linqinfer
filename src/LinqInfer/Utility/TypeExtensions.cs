using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Lists flags from an enum
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static  IEnumerable<Enum> GetFlags(this Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
            {
                if (input.HasFlag(value)) yield return value;
            }
        }

        /// <summary>
        /// Returns true if a type is an anonymous type
        /// </summary>
        public static bool IsAnonymous<T>()
        {
            var type = GetTypeInf<T>();
            return type.GetCustomAttributes(false).Any(a => a.GetType() == typeof(CompilerGeneratedAttribute))
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        /// <summary>
        /// Gets the inner type of a nullable type
        /// </summary>
        public static Type GetNullableTypeType(this Type type)
        {
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == nullableType)
            {
                return type.GetTypeInf().GetGenericArguments()[0];
            }
            return null;
        }

        /// <summary>
        /// Makes a value into a specific nullable type
        /// </summary>
        /// <param name="innerType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object MakeNullableType(this Type innerType, object value)
        {
            return nullableType
                .MakeGenericType(innerType)
                .GetTypeInfo()
                .GetConstructor(new Type[] { innerType })
                .Invoke(new object[] { value });
        }

        /// <summary>
        /// Converts a string into a specific type
        /// </summary>
        /// <param name="value">The string value</param>
        /// <param name="typeName">The type code name</param>
        /// <returns>An object</returns>
        public static object Parse(this string value, string typeName)
        {
            if (!Enum.TryParse(typeName, out TypeCode c)) return value;

            return Convert.ChangeType(value, c.ToType());
        }

        /// <summary>
        /// Returns the type code of a instance
        /// </summary>
        public static TypeCode GetTypeCode(this object objInstance)
        {
            if (objInstance == null) return TypeCode.Empty;

            var type = objInstance.GetType();

            return Type.GetTypeCode(type);
        }

        /// <summary>
        /// Returns true if a type is a simple numeric, date or string type. Null values will return false.
        /// </summary>
        public static bool IsPrimativeOrString(this object objInstance)
        {
            if (objInstance == null) return false;

            return IsPrimativeOrString(objInstance.GetType());
        }

        /// <summary>
        /// Returns true if a type is a simple numeric, date or string type
        /// </summary>
        public static bool IsPrimativeOrString(this Type type)
        {
            return Type.GetTypeCode(type) != TypeCode.Object;
        }

        /// <summary>
        /// Returns a type from a TypeCode
        /// </summary>
        public static Type ToType(this TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return typeof(bool);

                case TypeCode.Byte:
                    return typeof(byte);

                case TypeCode.Char:
                    return typeof(char);

                case TypeCode.DateTime:
                    return typeof(DateTime);

                case TypeCode.Decimal:
                    return typeof(decimal);

                case TypeCode.Double:
                    return typeof(double);

                case TypeCode.Empty:
                    return null;

                case TypeCode.Int16:
                    return typeof(short);

                case TypeCode.Int32:
                    return typeof(int);

                case TypeCode.Int64:
                    return typeof(long);

                case TypeCode.Object:
                    return typeof(object);

                case TypeCode.SByte:
                    return typeof(sbyte);

                case TypeCode.Single:
                    return typeof(Single);

                case TypeCode.String:
                    return typeof(string);

                case TypeCode.UInt16:
                    return typeof(UInt16);

                case TypeCode.UInt32:
                    return typeof(UInt32);

                case TypeCode.UInt64:
                    return typeof(UInt64);
            }

            return null;
        }
    }
}