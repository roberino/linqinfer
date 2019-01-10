using System.Collections.Generic;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    public interface IOneHotEncoding<T> : IExportableAsDataDocument
    {
        int VectorSize { get; }

        OneOfNVector Encode(T obj);

        BitVector Encode(IEnumerable<T> categories);

        IVector Encode(T[] categories);

        IEnumerable<KeyValuePair<T, int>> IndexTable { get; }
    }
}