using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace LinqInfer.Utility
{
    internal class ConcurrentQueue<T> : IEnumerable<T>, IDisposable
    {
        private readonly ManualResetEventSlim _dequeueWaitHandle;
        private readonly ManualResetEventSlim _enqueueWaitHandle;
        private readonly int _maxSize;
        private readonly Queue<T> _innerQueue;

        private bool _isClosed;

        public ConcurrentQueue(int maxSize = 4096)
        {
            Contract.Assert(maxSize > 0);

            _maxSize = maxSize;
            _innerQueue = new Queue<T>(maxSize);
            _dequeueWaitHandle = new ManualResetEventSlim();
            _enqueueWaitHandle = new ManualResetEventSlim();
        }

        public int Count
        {
            get
            {
                return _innerQueue.Count;
            }
        }

        public bool TryDequeueWhenAvailable(out T item)
        {
            if (WaitForItems(1))
            {
                item = Dequeue();

                return true;
            }

            item = default(T);

            return false;
        }

        public T Dequeue()
        {
            T next;

            lock (_innerQueue)
            {
                next = _innerQueue.Dequeue();
            }

            _dequeueWaitHandle.Set();
            _enqueueWaitHandle.Reset();

            return next;
        }

        public void Enqueue(T item)
        {
            if (_isClosed) throw new InvalidOperationException();

            while (_innerQueue.Count >= _maxSize)
            {
                _dequeueWaitHandle.Wait(500);
            }

            lock (_innerQueue)
            {
                _innerQueue.Enqueue(item);
            }

            _enqueueWaitHandle.Set();
        }

        public bool WaitForItems(int count)
        {
            while (_innerQueue.Count < count && !_isClosed)
            {
                _enqueueWaitHandle.Wait(500);
            }

            return _innerQueue.Count > 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _innerQueue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerQueue.GetEnumerator();
        }

        public void Close()
        {
            _isClosed = true;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
