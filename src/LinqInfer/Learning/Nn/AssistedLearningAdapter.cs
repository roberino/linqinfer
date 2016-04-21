using LinqInfer.Learning.Features;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Nn
{
    internal class AssistedLearningAdapter<TClass> : IAssistedLearning<TClass, double>
    {
        private readonly IAssistedLearningProcessor _processor;
        private readonly IFeatureExtractor<TClass, double> _outputVectorExtractor;

        public AssistedLearningAdapter(IAssistedLearningProcessor processor, IFeatureExtractor<TClass, double> outputVectorExtractor)
        {
            _processor = processor;
            _outputVectorExtractor = outputVectorExtractor;
        }

        public double Train(TClass item, double[] sample)
        {
            var output = new ColumnVector1D(_outputVectorExtractor.ExtractVector(item));

            var input = new ColumnVector1D(sample);

            return _processor.Train(input, output);
        }
    }
}
