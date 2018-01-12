using System.Collections.Generic;

namespace LinqInfer.Data
{
    internal sealed class Batch<T> : IBatch<T>
    {
        internal Batch(IList<T> items, int batchNumber)
        {
            Items = items;
            BatchNumber = batchNumber;
        }

        public int BatchNumber { get; }

        public IList<T> Items { get; }

        public bool IsLast { get; internal set; }

        public override string ToString()
        {
            return $"#{BatchNumber} ({Items.Count} items{(IsLast ? " - last batch" : "")})";
        }
    }
}