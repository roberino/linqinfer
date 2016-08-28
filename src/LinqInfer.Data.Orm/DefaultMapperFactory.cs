using System.Data;

namespace LinqInfer.Data.Orm
{
    internal class DefaultMapperFactory : IObjectMapperFactory
    {
        public IObjectMapper<T> Create<T>(DataTable schema)
        {
            return new ObjectMapper<T>(schema);
        }
    }
}
