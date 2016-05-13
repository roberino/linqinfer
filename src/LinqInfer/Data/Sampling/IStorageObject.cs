using System;

namespace LinqInfer.Data.Sampling
{
    public interface IStorageObject
    {
        string Label { get; }
        Uri Uri { get; }
    }
}
