using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface ICategoricalOutputMapper<T> : IFloatingPointFeatureExtractor<T> where T : IEquatable<T>
    {
        IEnumerable<T> OutputClasses { get; }
        void Initialise(IEnumerable<T> outputs);
        IEnumerable<ClassifyResult<T>> Map(IVector output);
    }
}