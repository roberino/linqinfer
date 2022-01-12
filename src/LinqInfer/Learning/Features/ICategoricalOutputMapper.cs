using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface ICategoricalOutputMapper<T> : IVectorFeatureExtractor<T> where T : IEquatable<T>
    {
        IEnumerable<T> OutputClasses { get; }
        IEnumerable<ClassifyResult<T>> Map(IVector output);
    }
}