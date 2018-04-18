using System;
using System.Collections.Generic;

namespace LinqInfer.Utility
{
    internal class ObjectPool<T> where T : class
    {
        private readonly object _lockObj = new object();
        private readonly Stack<T> _objectStack;
        private readonly Func<ObjectPool<T>, T> _objectFactory;

        private readonly Action<T> _resetAction;

        public ObjectPool(
            int initialBufferSize,
            Func<ObjectPool<T>, T> objectFactory,
            Action<T> resetAction = null)
        {
            _objectStack = new Stack<T>(initialBufferSize);
            _objectFactory = objectFactory;
            _resetAction = resetAction;
        }

        public T New()
        {
            lock (_lockObj)
            {
                if (_objectStack.Count > 0)
                {
                    T t = _objectStack.Pop();

                    _resetAction?.Invoke(t);

                    return t;
                }
                else
                {
                    T t = _objectFactory(this);

                    return t;
                }
            }
        }

        public void Reuse(T obj)
        {
            lock (_lockObj)
            {
                _objectStack.Push(obj);
            }
        }
    }
}