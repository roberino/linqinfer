using System;

namespace LinqInfer.Data.Sampling
{
    public interface IStorageObject : IEntity
    {
        string Label { get; }
        Uri Uri { get; }
    }
}
