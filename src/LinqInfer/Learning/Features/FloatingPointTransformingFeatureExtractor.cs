using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Features
{
    internal class FloatingPointTransformingFeatureExtractor<TInput> : TransformingFeatureExtractor<TInput, double>, IFloatingPointFeatureExtractor<TInput>
    {
        public FloatingPointTransformingFeatureExtractor(IFloatingPointFeatureExtractor<TInput> baseFeatureExtractor, Func<double[], double[]> transformation, int[] indexSelection = null) : base(baseFeatureExtractor, transformation, indexSelection)
        {
        }

        public ColumnVector1D ExtractColumnVector(TInput obj)
        {
            return new ColumnVector1D(base.ExtractVector(obj));
        }
    }
}
