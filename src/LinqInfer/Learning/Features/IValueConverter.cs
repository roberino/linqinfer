using System;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Interface for converting a CLR type to a floating point number.
    /// </summary>
    public interface IValueConverter
    {
        bool CanConvert(Type type);
        double Convert(object value);
    }

    public interface IDefaultValueConverter : IValueConverter
    {
    }
}