using System;

namespace LinqInfer.Learning.Features
{
    public interface IValueConverter
    {
        bool CanConvert(Type type);
        float Convert(object value);
    }
}
