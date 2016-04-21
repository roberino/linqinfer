using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetworkClassificationPipeline<TClass, TInput> : ClassificationPipeline<TClass, TInput, double> where TClass : IEquatable<TClass>
    {
        private readonly Config _config;
        private readonly double _toleranceThreshold = 1.1;

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

        public override double Train(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf)
        {
            int hiddenLayerFactor = 8;
            IList<TClass> outputs = null;

            if (_config.OutputMapper.VectorSize == 0)
            {
                outputs = trainingData.GroupBy(classf).Select(o => o.Key).ToList();

                _config.OutputMapper.Initialise(outputs);

                _config.Network.Initialise(_config.FeatureExtractor.VectorSize, _config.FeatureExtractor.VectorSize * hiddenLayerFactor, outputs.Count);
            }

            _config.FeatureExtractor.CreateNormalisingVector(trainingData);
            
            int i = 0;
            double error = 0;
            double lastError = 0;

            while (i < 1000)
            {
                error = base.Train(trainingData, classf);

                if (error < _toleranceThreshold) break;

                if (lastError == error)
                {
                    if (hiddenLayerFactor == 1) break;

                    hiddenLayerFactor--;
                    lastError = 0;

                    _config.Network.Initialise(_config.FeatureExtractor.VectorSize, _config.FeatureExtractor.VectorSize * hiddenLayerFactor, outputs.Count);
                }

                lastError = error;
            }

            return error;
        }

        private static Config Setup(
            IFeatureExtractor<TInput, double> featureExtractor,
            IOutputMapper<TClass> outputMapper,
            TInput normalisingSample)
        {
            if (outputMapper == null) outputMapper = new OutputMapper<TClass>();
            var network = new MultilayerNetwork(featureExtractor.VectorSize);
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