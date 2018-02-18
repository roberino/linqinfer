using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public sealed class AsyncBatch<T> : IBatch<T>
    {
        private readonly Lazy<IList<T>> _items;

        public AsyncBatch(Task<IList<T>> itemsLoader, bool isLast, int batchNum)
        {
            ItemsLoader = itemsLoader;
            IsLast = isLast;
            BatchNumber = batchNum;
            _items = new Lazy<IList<T>>(() => ItemsLoader.GetAwaiter().GetResult());
        }

        public Task<IList<T>> ItemsLoader { get; }

        public IList<T> Items => _items.Value;

        public int BatchNumber { get; }

        public bool IsLast { get; internal set; }
    }
}