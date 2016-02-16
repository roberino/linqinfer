using System;

namespace LinqInfer.Learning
{
    public interface INeuron<T>
    {
        Func<T, double> Pdf { get; }
        void AddSample(T data);
    }
}
