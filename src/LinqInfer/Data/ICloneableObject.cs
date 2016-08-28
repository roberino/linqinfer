using System;
using System.Runtime;

namespace LinqInfer.Data
{
    public interface ICloneableObject<T> // : ICloneable
    {
        T Clone(bool deep);
    }
}