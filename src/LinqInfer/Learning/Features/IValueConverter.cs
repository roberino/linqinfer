using System;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Interface for converting a CLR type to a floating point number.
    /// </summary>
    public interface IValueConverter
    {
        bool CanConvert(Type type);
        float Convert(object value);
    }
}
