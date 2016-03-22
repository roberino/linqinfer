using System;

namespace LinqInfer.Storage
{
    public interface IStorageObject
    {
        string Label { get; }
        Uri Uri { get; }
    }
}
