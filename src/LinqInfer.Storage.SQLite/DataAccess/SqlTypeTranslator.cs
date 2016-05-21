using System;
using System.IO;
using System.Xml;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    public class SqlTypeTranslator
    {
        public string TranslateToSqlTypeName(Type type)
        {
            var tc = Type.GetTypeCode(type);

            switch (tc)
            {
                case TypeCode.String:
                case TypeCode.DateTime:
                    return "text";
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                    return "integer";
                case TypeCode.Double:
                case TypeCode.Single:
                    return "real";
                default:
                    if (IsStream(type) || type == typeof(byte[]))
                    {
                        return "blob";
                    }
                    return "numeric";
            }
        }

        public object ConvertToSqlValue(object value)
        {
            if (value == null) return DBNull.Value;

            var type = value.GetType();
            var tc = Type.GetTypeCode(type);

            switch (tc)
            {
                case TypeCode.DateTime:
                    return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Utc);
                default:
                    if (tc == TypeCode.Object && IsStream(type))
                    {
                        return ReadStream((Stream)value);
                    }

                    return value;
            }
        }

        public object ConvertToClrValue(object value, Type clrType)
        {
            if (value == DBNull.Value) return null;

            var type = value.GetType();
            var tc = Type.GetTypeCode(type);

            switch (tc)
            {
                case TypeCode.DateTime:
                    return XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.Utc);
                default:
                    if (tc == TypeCode.Object && IsStream(type))
                    {
                        return new MemoryStream((byte[])value);
                    }

                    return value;
            }
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
