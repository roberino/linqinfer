using LinqInfer.Learning.Features;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetworkClassifier<TClass, TInput> : ClassificationPipeline<TClass, TInput, double>
    {
        private readonly Config _config;

        public MultilayerNetworkClassifier(
            IFeatureExtractor<TInput, double> featureExtractor,
            IFeatureExtractor<TClass, double> outputExtractor,
            TInput normalisingSample = default(TInput)) : this(Setup(featureExtractor, outputExtractor, normalisingSample))
        {
        }

        private MultilayerNetworkClassifier(Config config) : base(config.Trainer, config.Classifier, config.FeatureExtractor, config.NormalisingSample)
        {
            _config = config;
        }

        private static Config Setup(
            IFeatureExtractor<TInput, double> featureExtractor,
            IFeatureExtractor<TClass, double> outputExtractor,
            TInput normalisingSample)
        {
            var network = new MultilayerNetwork(featureExtractor.VectorSize, new int[] { featureExtractor.VectorSize, featureExtractor.VectorSize });
            var bpa = new BackPropagationLearning(network);
            var adapter = new AssistedLearningAdapter<TClass>(bpa, outputExtractor);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, null);

            return new Config()
            {
                NormalisingSample = normalisingSample,
                Network = network,
                Trainer = adapter,
                FeatureExtractor = featureExtractor,
                Classifier = classifier
            };
        }

        private class Config
        {
            public  TInput NormalisingSample { get; set; }
            public IClassifier<TClass, double> Classifier { get; set; }
            public IFeatureExtractor<TInput, double> FeatureExtractor { get; set; }
            public MultilayerNetwork Network { get; set; }
            public IAssistedLearning<TClass, double> Trainer { get; set; }
        }
    }
}