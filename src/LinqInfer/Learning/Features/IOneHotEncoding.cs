using System.Collections.Generic;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    public interface IOneHotEncoding<T> : IExportableAsDataDocument
    {
        int VectorSize { get; }

        bool HasEntry(T obj);

        OneOfNVector Encode(T obj);

        BitVector Encode(IEnumerable<T> categories);

        IVector Encode(T[] categories);

        T GetEntry(int index);

        IEnumerable<KeyValuePair<T, int>> IndexTable { get; }
    }
}