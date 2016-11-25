using System;

namespace LinqInfer.Data
{
    public class EventArgsOf<T> : EventArgs
    {
        public EventArgsOf(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }
    }
}
