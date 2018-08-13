using System;
using System.Xml;

namespace LinqInfer.Utility
{
    class GenericTypeConverter<T>
    {
        readonly Func<string, T> _convertFromString;
        readonly Func<T, string> _convertToString;
        readonly TypeCode _typeCode;

        public GenericTypeConverter()
        {
            var typeInf = typeof(T);

            _typeCode = Type.GetTypeCode(typeInf);
            _convertToString = v => v.ToString().ToLower();

            switch (_typeCode)
            {
                case TypeCode.Boolean:
                    _convertFromString = (v) => (T)(object)bool.Parse(v);
                    break;
                case TypeCode.Int16:
                    _convertFromString = (v) => (T)(object)short.Parse(v);
                    break;
                case TypeCode.Int32:
                    _convertFromString = (v) => (T)(object)int.Parse(v);
                    break;
                case TypeCode.Int64:
                    _convertFromString = (v) => (T)(object)long.Parse(v);
                    break;
                case TypeCode.Single:
                    _convertFromString = (v) => (T)(object)float.Parse(v);
                    break;
                case TypeCode.Double:
                    _convertFromString = (v) => (T)(object)double.Parse(v);
                    break;
                case TypeCode.Decimal:
                    _convertFromString = (v) => (T)(object)decimal.Parse(v);
                    break;
                case TypeCode.Char:
                    _convertFromString = (v) => (T)(object)v[0];
                    break;
                case TypeCode.String:
                    _convertFromString = (v) => (T)(object)v;
                    break;
                case TypeCode.DateTime:
                    _convertFromString = (v) => (T)(object)XmlConvert.ToDateTime(v, XmlDateTimeSerializationMode.Utc);
                    _convertToString = v => XmlConvert.ToString((DateTime)(object)v, XmlDateTimeSerializationMode.Utc);
                    break;
                default:
                    throw new NotSupportedException(_typeCode.ToString());
            }
        }

        public string ConvertToString(T value)
        {
            return _convertToString(value);
        }

        public T ConvertFromString(string value)
        {
            return _convertFromString(value);
        }
    }
}