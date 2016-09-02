using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Utility
{
    internal class AsyncQueryable<T> : IQueryable<T>
    {
        private readonly Func<Task<IEnumerable<T>>> _dataLoader;
        private readonly List<AsyncEnumerator> _enumerators;
        private readonly List<Task> _waitTasks;

        public AsyncQueryable(Func<Task<IEnumerable<T>>> dataLoader)
        {
            _dataLoader = dataLoader;
            _enumerators = new List<AsyncEnumerator>();

            var objQ = Enumerable.Empty<T>().AsQueryable();

            ElementType = objQ.ElementType;
            Expression = objQ.Expression;
            Provider = objQ.Provider;
        }

        public Type ElementType { get; private set; }

        public Expression Expression { get; private set; }

        public IQueryProvider Provider { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            var e = new AsyncEnumerator();

            _enumerators.Add(e);

            var task = new Task(async () =>
            {
                while (!e.Closed)
                {
                    var d = await _dataLoader();

                    if (d.Any())
                    {
                        foreach (var x in d) e.Push(x);
                    }
                    else
                    {
                        e.Close();
                        return;
                    }
                }
            });

            _waitTasks.Add(task);

            task.Start();

            return e;
        }

        public async Task Wait()
        {
            foreach (var task in _waitTasks.Where(t => !(t.IsCompleted || t.IsCanceled || t.IsFaulted)))
            {
                await task;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class AsyncEnumerator : IEnumerator<T>
        {
            private readonly Queue<T> _buffer;
            private bool _closed;
            private ManualResetEvent _waitHandle;
            private T _current;

            public AsyncEnumerator()
            {
                _buffer = new Queue<T>();
                _waitHandle = new ManualResetEvent(false);
            }

            public void Push(T item)
            {
                lock (_buffer) _buffer.Enqueue(item);
                _waitHandle.Set();
            }

            public bool Closed
            {
                get { return _closed; }
            }

            public void Close()
            {
                _closed = true;
            }

            public T Current
            {
                get
                {
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
                _closed = true;
            }

            public bool MoveNext()
            {
                if (_closed) return false;

                lock (_buffer)
                {
                    if (_buffer.Count > 0)
                    {
                        var next = _buffer.Dequeue();
                        _current = next;
                        return true;
                    }
                }

                _waitHandle.WaitOne(5000);

                return MoveNext();
            }

            public void Reset()
            {
            }
        }
    }
}