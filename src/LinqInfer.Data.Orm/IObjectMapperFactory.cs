using System.Data;

namespace LinqInfer.Data.Orm
{
    public interface IObjectMapperFactory
    {
        IObjectMapper<T> Create<T>(DataTable schema);
    }
}
