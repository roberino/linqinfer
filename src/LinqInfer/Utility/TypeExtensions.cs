using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqInfer.Utility
{
    static class TypeExtensions
    {
        static readonly Type NullableType = typeof(Nullable<>);
        static readonly HashSet<Type> FuncTypes = new HashSet<Type>(new[] {typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>), typeof(Func<,,,,,>), typeof(Func<,,,,,,>), typeof(Func<,,,,,,,>), typeof(Func<,,,,,,,,>), typeof(Func<,,,,,,,,,>), typeof(Func<,,,,,,,,,,>)});

        public static Type GetFuncType(int numberOfArgs)
        {
            return FuncTypes.Single(f => f.GetGenericArguments().Length == numberOfArgs + 1);
        }

        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            return obj
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(k => k.Name, v => v.GetValue(obj));
        }

        public static string ToDictionaryString<T>(this IDictionary<string, T> values)
        {
            return values
                .Aggregate(new StringBuilder(), (s, kv) => s.Append($"{kv.Key}={Regex.Escape(kv.Value?.ToString() ?? string.Empty)}|"))
                .ToString();
        }

        public static IDictionary<string, T> FromDictionaryString<T>(this string data)
        {
            var type = typeof(T);

            return data
                .Split('|')
                .Where(a => a.Trim().Length > 0)
                .Select(a => a.Split('='))
                .Select(v => new {k = v[0], v = v[1]})
                .ToDictionary(x => x.k.ToString(), x => (T) Convert.ChangeType(x.v, type));
        }

        public static T ToObject<T>(this IDictionary<string, object> properties)
            where T : new()
        {
            var result = new T();

            var writable = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var prop in writable)
            {
                if (properties.TryGetValue(prop.Name, out object val))
                {
                    object res;

                    if (prop.PropertyType.IsEnum && val is string)
                    {
                        res = Enum.Parse(prop.PropertyType, val.ToString());
                    }
                    else
                    {
                        res = Convert.ChangeType(val, prop.PropertyType);
                    }

                    prop.SetValue(result, res);
                }
            }

            return result;
        }

        public static IEnumerable<Type> FindTypes<T>(this Assembly asm, Func<Type, bool> predicate = null)
        {
            var searchType = typeof(T).GetTypeInf();

            return asm
                .ExportedTypes
                .Where(t => searchType.IsAssignableFrom(t) && (predicate?.Invoke(t)).GetValueOrDefault(true));
        }

        public static Func<TArg, T> FindFactory<TArg, T>(this Type type)
            where T : class
        {
            if (type == null) type = typeof(T);
            var argType = typeof(TArg);
            var allMethods = type.GetTypeInf().GetMethods(BindingFlags.Static | BindingFlags.Public);

            var matching = allMethods.Where(m => m.ReturnType == type && m.GetParameters().Length == 1 && m.GetParameters().Single().ParameterType == argType);

            var match = matching.Single();

            return x => match.Invoke(null, new object[] { x }) as T;
        }

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
        public static bool IsFunc(this Type type)
        {
            return type.Namespace == typeof(Func<>).Namespace && type.IsGenericType && FuncTypes.Contains(type.GetGenericTypeDefinition());
        }

        /// <summary>
        /// Returns true if a type is an anonymous type
        /// </summary>
        public static bool IsAnonymous<T>()
        {
            var type = GetTypeInf<T>();
            return type.GetCustomAttributes(false).Any(a => a is CompilerGeneratedAttribute)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        /// <summary>
        /// Gets the inner type of a nullable type
        /// </summary>
        public static Type GetNullableTypeType(this Type type)
        {
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == NullableType)
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
            return NullableType
                .MakeGenericType(innerType)
                .GetTypeInfo()
                .GetConstructor(new [] { innerType })
                ?.Invoke(new [] { value });
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
        /// Returns true if a type has generic parameters
        /// </summary>
        public static bool HasGenericParameters(this Type type)
        {
            if (type.IsGenericParameter)
            {
                return true;
            }

            if (type.IsGenericType)
            {
                return type.GenericTypeArguments.Any(HasGenericParameters);
            }

            return false;
        }

        /// <summary>
        /// Returns true for numeric type codes
        /// </summary>
        public static bool IsNumeric(this TypeCode tc)
        {
            switch (tc)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
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

        public static bool IsCompatibleArrayAndGeneric(this Type argType, Type genericType)
        {
            if (genericType.IsGenericTypeDefinition && argType.IsSubclassOf(typeof(Array)))
            {   
                return genericType == typeof(IEnumerable<>) || genericType == typeof(IList<>) || genericType == typeof(ICollection<>);
            }

            return false;
        }

        internal static Type GetTypeInf<T>()
        {
            return typeof(T);
        }

        internal static Type GetTypeInf(this Type type)
        {
            return type;
        }
    }
}