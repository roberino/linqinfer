using System.Collections.Generic;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    public interface IOneHotEncoding<T> : IExportableAsDataDocument
    {
        int VectorSize { get; }

        OneOfNVector Encode(T obj);
        
        IEnumerable<KeyValuePair<T, int>> IndexTable { get; }
    }
}