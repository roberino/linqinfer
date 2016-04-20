using LinqInfer.Learning.Features;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetworkClassificationPipeline<TClass, TInput> : ClassificationPipeline<TClass, TInput, double> where TClass : IEquatable<TClass>
    {
        private readonly Config _config;

        public MultilayerNetworkClassificationPipeline(
            IFeatureExtractor<TInput, double> featureExtractor,
            IOutputMapper<TClass> outputMapper = null,
            TInput normalisingSample = default(TInput)) : this(Setup(featureExtractor, outputMapper, normalisingSample))
        {
        }

        private MultilayerNetworkClassificationPipeline(Config config) : base(config.Trainer, config.Classifier, config.FeatureExtractor, config.NormalisingSample)
        {
            _config = config;
        }

        public override void Train(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf)
        {
            if (_config.OutputMapper.VectorSize == 0)
            {
                var outputs = trainingData.GroupBy(classf);

                _config.OutputMapper.Initialise(outputs.Select(o => o.Key).ToList());
            }

            _config.FeatureExtractor.CreateNormalisingVector(trainingData);

            base.Train(trainingData, classf);
        }

        private static Config Setup(
            IFeatureExtractor<TInput, double> featureExtractor,
            IOutputMapper<TClass> outputMapper,
            TInput normalisingSample)
        {
            if (outputMapper == null) outputMapper = new OutputMapper<TClass>();
            var network = new MultilayerNetwork(featureExtractor.VectorSize, new int[] { featureExtractor.VectorSize, featureExtractor.VectorSize / 2 });
            var bpa = new BackPropagationLearning(network);
            var adapter = new AssistedLearningAdapter<TClass>(bpa, outputMapper);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, outputMapper.Map);
            
            return new Config()
            {
                NormalisingSample = normalisingSample,
                Network = network,
                Trainer = adapter,
                FeatureExtractor = featureExtractor,
                Classifier = classifier,
                OutputMapper = outputMapper
            };
        }

        private class Config
        {
            public  TInput NormalisingSample { get; set; }
            public IOutputMapper<TClass> OutputMapper { get; set; }
            public IClassifier<TClass, double> Classifier { get; set; }
            public IFeatureExtractor<TInput, double> FeatureExtractor { get; set; }
            public MultilayerNetwork Network { get; set; }
            public IAssistedLearning<TClass, double> Trainer { get; set; }
        }
    }
}