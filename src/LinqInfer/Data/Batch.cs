using System.Collections.Generic;

namespace LinqInfer.Data
{
    public sealed class Batch<T> : IBatch<T>
    {
        internal Batch(IList<T> items, int batchNumber)
        {
            Items = items;
            BatchNumber = batchNumber;
        }

        public int BatchNumber { get; }

        public IList<T> Items { get; }

        public bool IsLast { get; internal set; }
    }
}