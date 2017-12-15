using LinqInfer.Learning.Features;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    internal class AssistedLearningAdapter<TClass> : IAssistedLearningProcessor<TClass, double>
    {
        private readonly IAssistedLearningProcessor _processor;
        private readonly IFeatureExtractor<TClass, double> _outputVectorExtractor;

        public AssistedLearningAdapter(IAssistedLearningProcessor processor, IFeatureExtractor<TClass, double> outputVectorExtractor)
        {
            _processor = processor;
            _outputVectorExtractor = outputVectorExtractor;
        }

        public double Train(TClass sampleClass, ColumnVector1D sample)
        {
            var output = new ColumnVector1D(_outputVectorExtractor.ExtractVector(sampleClass));

            return _processor.Train(sample, output);
        }

        public double Train(TClass sampleClass, double[] sample)
        {
            return Train(sampleClass, new ColumnVector1D(sample));
        }
    }
}
