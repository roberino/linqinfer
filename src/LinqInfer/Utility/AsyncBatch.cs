using System.Collections.Generic;

namespace LinqInfer.Utility
{
    public sealed class AsyncBatch<T>
    {
        internal AsyncBatch(IList<T> items, int batchNumber)
        {
            Items = items;
            BatchNumber = batchNumber;
        }

        public int BatchNumber { get; }

        public IList<T> Items { get; }
    }
}