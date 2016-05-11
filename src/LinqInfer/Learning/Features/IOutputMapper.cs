using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    interface IOutputMapper<T> : IFeatureExtractor<T, double> where T : IEquatable<T>
    {
        void Initialise(IEnumerable<T> outputs);
        IEnumerable<ClassifyResult<T>> Map(ColumnVector1D output);
    }
}