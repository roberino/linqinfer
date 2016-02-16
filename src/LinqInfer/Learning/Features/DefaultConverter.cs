using System;

namespace LinqInfer.Learning.Features
{
    public class DefaultConverter : IValueConverter
    {
        public DefaultConverter() { }

        public bool CanConvert(Type type)
        {
            return CanConvertToVector(type);
        }

        public float Convert(object value)
        {
            if (value == null) return 0f;
            if (value is bool) return ((bool)value) ? 1f : 0f;
            if (value is DateTime) return (float)(((DateTime)value) - new DateTime(1900, 01, 01)).TotalMinutes;

            return System.Convert.ToSingle(value);
        }

        private bool CanConvertToVector(Type type)
        {
            var tc = Type.GetTypeCode(type);

            return tc == TypeCode.Boolean ||
                tc == TypeCode.Byte ||
                tc == TypeCode.Decimal ||
                tc == TypeCode.Double ||
                tc == TypeCode.Int16 ||
                tc == TypeCode.Int32 ||
                tc == TypeCode.Int64 ||
                tc == TypeCode.Single ||
                tc == TypeCode.DateTime;
        }
    }
}
