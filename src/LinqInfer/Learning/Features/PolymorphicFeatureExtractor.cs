using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    class PolymorphicFeatureExtractor<TInput> : TypeMapper<TInput>, IVectorFeatureExtractor<TInput>
    {
        public PolymorphicFeatureExtractor(int vectorSize) : base(vectorSize)
        {
        }

        public bool CapacityReached => VectorSize <= CurrentSize;

        public bool CanEncode(TInput obj) => true;

        public IVector ExtractIVector(TInput obj)
        {
            var map = GetOrCreateMap(obj.GetType());

            var data = new double[VectorSize];

            foreach (var prop in map.Properties)
            {
                data[prop.Index] = prop.GetVectorValue(obj);
            }

            return new ColumnVector1D(data);
        }
    }
}