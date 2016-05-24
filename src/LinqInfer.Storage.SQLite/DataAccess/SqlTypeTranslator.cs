using System;
using System.IO;
using System.Xml;
using LinqInfer.Utility;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    public class SqlTypeTranslator
    {
        public bool CanConvertToSql(Type type)
        {
            var innerType = type.GetNullableTypeType() ?? type;

            return Type.GetTypeCode(innerType) != TypeCode.Object || IsByteArray(type) || IsStream(type) || IsUri(type) || innerType.IsEnum;
        }

        public string TranslateToSqlTypeName(Type innerType)
        {
            var tc = Type.GetTypeCode(innerType);

            if (innerType.IsEnum) return "text";

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
                    if (IsStream(innerType) || IsByteArray(innerType)) return "blob";
                    if (IsUri(innerType)) return "text";
                    return "numeric";
            }
        }

        public object ConvertToSqlValue(object value)
        {
            if (value == null) return DBNull.Value;

            var type = value.GetType();
            var innerType = type.GetNullableTypeType();
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

        public object ConvertToClrValue(object value, Type innerClrType, bool isNullable)
        {
            if (value == DBNull.Value) return null;

            var tc = Type.GetTypeCode(innerClrType);

            if (innerClrType.IsEnum)
            {
                var val = Enum.Parse(innerClrType, (string)value);

                if (isNullable)
                {
                    return innerClrType.MakeNullableType(val);
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
                        return innerClrType.MakeNullableType(Convert.ChangeType(value, innerClrType));
                    }

                    if (tc == TypeCode.Object)
                    {
                        if (IsStream(innerClrType)) return new MemoryStream((byte[])value);
                        if (IsUri(innerClrType)) return new Uri((string)value);
                    }

                    return value;
            }
        }

        public object ConvertToClrValue(object value, Type clrType)
        {
            var innerType = clrType.GetNullableTypeType();

            return ConvertToClrValue(value, innerType ?? clrType, innerType != null);
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
