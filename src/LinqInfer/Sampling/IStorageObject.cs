using System;

namespace LinqInfer.Sampling
{
    public interface IStorageObject
    {
        string Label { get; }
        Uri Uri { get; }
    }
}
