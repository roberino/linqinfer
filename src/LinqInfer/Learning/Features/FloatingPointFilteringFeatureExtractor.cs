using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Features
{
    internal class FloatingPointFilteringFeatureExtractor<TInput> : TransformingFeatureExtractor<TInput, double>, IHasVectorTransformation, IFloatingPointFeatureExtractor<TInput>
    {
        public FloatingPointFilteringFeatureExtractor(IFloatingPointFeatureExtractor<TInput> baseFeatureExtractor, Func<IFeature, bool> featureFilter = null) : base(baseFeatureExtractor, null, featureFilter)
        {
        }

        public IVectorTransformation VectorTransformation
        {
            get
            {
                return new DelegateVectorTransformation(InputSize, v => new Vector(Transformation(v.GetUnderlyingArray())));
            }
        }

        public ColumnVector1D ExtractColumnVector(TInput obj)
        {
            return new ColumnVector1D(base.ExtractVector(obj));
        }
    }
}