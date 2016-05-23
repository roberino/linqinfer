using System;
using System.IO;
using System.Xml;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    public class SqlTypeTranslator
    {
        private static readonly Type nullableType = typeof(Nullable<>);

        public bool CanConvertToSql(Type type)
        {
            return Type.GetTypeCode(type) != TypeCode.Object || IsByteArray(type) || IsStream(type) || IsUri(type) || type.IsEnum;
        }

        public Type GetNullableTypeType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == nullableType)
            {
                return type.GetGenericArguments()[0];
            }
            return null;
        }

        public string TranslateToSqlTypeName(Type type)
        {
            var innerType = GetNullableTypeType(type) ?? type;
            var tc = Type.GetTypeCode(innerType);

            if (type.IsEnum) return "text";

            switch (tc)
            {
                case TypeCode.String:
                case TypeCode.DateTime:
                    return "text";
                case TypeCode.Boolean:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                    return "integer";
                case TypeCode.Double:
                case TypeCode.Single:
                    return "real";
                default:
                    if (IsStream(type) || IsByteArray(type)) return "blob";
                    if (IsUri(type)) return "text";
                    return "numeric";
            }
        }

        public object ConvertToSqlValue(object value)
        {
            if (value == null) return DBNull.Value;

            var type = value.GetType();
            var innerType = GetNullableTypeType(type);
            var isNullable = innerType != null;
            var tc = Type.GetTypeCode(innerType ?? type);

            switch (tc)
            {
                case TypeCode.DateTime:
                    if (isNullable)
                    {
                        return XmlConvert.ToString(((DateTime?)value).Value, XmlDateTimeSerializationMode.Utc);
                    }
                    else
                    {
                        return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Utc);
                    }
                case TypeCode.Boolean:
                    if (isNullable)
                    {
                        return ((bool?)value).Value ? 1 : 0;
                    }
                    return ((bool)value) ? 1 : 0;
                default:

                    if ((innerType ?? type).IsEnum)
                    {
                        return value.ToString();
                    }

                    if (tc == TypeCode.Object)
                    {
                        if (IsStream(type)) return ReadStream((Stream)value);
                        if (IsUri(type)) return ((Uri)value).ToString();
                    }

                    return value;
            }
        }

        public object ConvertToClrValue(object value, Type clrType)
        {
            if (value == DBNull.Value) return null;

            var type = clrType;
            var innerType = GetNullableTypeType(type);
            var isNullable = innerType != null;
            var tc = Type.GetTypeCode(innerType ?? type);
            
            if ((innerType ?? type).IsEnum)
            {
                var val = Enum.Parse((type ?? innerType), (string)value);

                if (isNullable)
                {
                    return MakeNullableType(innerType, val);
                }

                return val;
            }

            switch (tc)
            {
                case TypeCode.DateTime:
                    {
                        var val = XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.Utc);

                        if (isNullable)
                        {
                            return new DateTime?(val);
                        }

                        return val;
                    }
                case TypeCode.Boolean:
                    {
                        var val = (long)value;

                        if (isNullable)
                        {
                            return new bool?(val > 0);
                        }

                        return val > 0;
                    }
                case TypeCode.Byte:
                    {
                        var val = (byte)(long)value;

                        if (isNullable)
                        {
                            return new byte?(val);
                        }

                        return val;
                    }
                case TypeCode.Int16:
                    {
                        var val = (short)(long)value;

                        if (isNullable)
                        {
                            return new short?(val);
                        }

                        return val;
                    }
                case TypeCode.Int32:
                    {
                        var val = (int)(long)value;

                        if (isNullable)
                        {
                            return new int?(val);
                        }

                        return val;
                    }
                case TypeCode.Single:
                    {
                        var val = (float)(double)value;

                        if (isNullable)
                        {
                            return new float?(val);
                        }

                        return val;
                    }
                default:

                    if (isNullable)
                    {
                        return MakeNullableType(innerType, Convert.ChangeType(value, innerType));
                    }

                    if (tc == TypeCode.Object)
                    {
                        if (IsStream(type)) return new MemoryStream((byte[])value);
                        if (IsUri(type)) return new Uri((string)value);
                    }

                    return value;
            }
        }

        private static object MakeNullableType(Type innerType, object value)
        {
            return nullableType
                .MakeGenericType(innerType)
                .GetConstructor(new Type[] { innerType })
                .Invoke(new object[] { value });
        }

        private static bool IsUri(Type type)
        {
            return type == typeof(Uri);
        }

        private static bool IsByteArray(Type type)
        {
            return type == typeof(byte[]);
        }

        private static bool IsStream(Type type)
        {
            return typeof(Stream).IsAssignableFrom(type);
        }

        private static byte[] ReadStream(Stream stream)
        {
            if (stream is MemoryStream)
            {
                return ((MemoryStream)stream).ToArray();
            }

            using(var ms = new MemoryStream())
            {
                stream.CopyTo(ms);

                return ms.ToArray();
            }
        }
    }
}
