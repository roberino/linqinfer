using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Features
{
    internal class FloatingPointTransformingFeatureExtractor<TInput> : TransformingFeatureExtractor<TInput, double>, IFloatingPointFeatureExtractor<TInput>
    {
        public FloatingPointTransformingFeatureExtractor(IFloatingPointFeatureExtractor<TInput> baseFeatureExtractor, Func<double[], double[]> transformation, Func<IFeature, bool> featureFilter = null) : base(baseFeatureExtractor, transformation, featureFilter)
        {
        }

        public ColumnVector1D ExtractColumnVector(TInput obj)
        {
            return new ColumnVector1D(base.ExtractVector(obj));
        }
    }
}
