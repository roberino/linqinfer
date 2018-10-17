using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    class PolymorphicFeatureExtractor<T> : TypeMapper<T>, IFloatingPointFeatureExtractor<T>
    {
        public PolymorphicFeatureExtractor(int vectorSize) : base(vectorSize)
        {
        }

        public bool CapacityReached => VectorSize <= CurrentSize;

        public IVector ExtractIVector(T obj)
        {
            var map = GetOrCreateMap(obj.GetType());

            var data = new double[VectorSize];

            foreach (var prop in map.Properties)
            {
                data[prop.Index] = prop.GetVectorValue(obj);
            }

            return new ColumnVector1D(data);
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }
    }
}