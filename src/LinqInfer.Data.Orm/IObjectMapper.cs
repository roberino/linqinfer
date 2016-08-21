using System;

namespace LinqInfer.Data.Orm
{
    public interface IObjectMapper<T>
    {
        void MapProperty(T instance, Type propertyType, string propertyName, object value);
    }
}
