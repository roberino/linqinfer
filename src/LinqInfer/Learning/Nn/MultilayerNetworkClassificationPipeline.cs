using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetworkClassificationPipeline<TClass, TInput> where TClass : IEquatable<TClass>
    {
        private readonly Config _config;
        private readonly double _toleranceThreshold = 0.9;

        private ClassificationPipeline<TClass, TInput, double> _pipeline;

        public MultilayerNetworkClassificationPipeline(
            IFeatureExtractor<TInput, double> featureExtractor,
            IOutputMapper<TClass> outputMapper = null,
            TInput normalisingSample = default(TInput)) : this(Setup(featureExtractor, outputMapper, normalisingSample))
        {
        }

        private MultilayerNetworkClassificationPipeline(Config config) // : base(config.Trainer, config.Classifier, config.FeatureExtractor, config.NormalisingSample)
        {
            _config = config;
        }

        public double Train(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf)
        {
            int hiddenLayerFactor = 8;
            IList<TClass> outputs = null;

            if (_config.OutputMapper.VectorSize == 0)
            {
                outputs = trainingData.GroupBy(classf).Select(o => o.Key).ToList();

                _config.OutputMapper.Initialise(outputs);
                _config.FeatureExtractor.CreateNormalisingVector(trainingData);
            }

            var networks = new List<Tuple<ClassificationPipeline<TClass, TInput, double>, double>>();

            Enumerable.Range(0, hiddenLayerFactor).AsParallel().ForAll(n =>
            {
                networks.Add(TrainNetwork(trainingData, classf, n, outputs.Count));
            });

            var min = networks.OrderBy(x => x.Item2).First();

            _pipeline = min.Item1;

            return min.Item2;
        }

        public ClassifyResult<TClass> Classify(TInput obj)
        {
            if (_pipeline == null) throw new InvalidOperationException("Pipeline not trained");

            return _pipeline.Classify(obj);
        }

        private Tuple<ClassificationPipeline<TClass, TInput, double>, double> TrainNetwork(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf, int hiddenLayerFactor, int outputSize)
        {
            var network = new MultilayerNetwork(_config.FeatureExtractor.VectorSize);
            var bpa = new BackPropagationLearning(network);
            var learningAdapter = new AssistedLearningAdapter<TClass>(bpa, _config.OutputMapper);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, _config.OutputMapper.Map);

            var pipeline = new ClassificationPipeline<TClass, TInput, double>(learningAdapter, classifier, _config.FeatureExtractor, _config.NormalisingSample);

            network.Initialise(_config.FeatureExtractor.VectorSize, _config.FeatureExtractor.VectorSize / 2 * hiddenLayerFactor, outputSize);

            int i = 0;
            double error = 0;
            double lastError = 0;

            while (i < 1000)
            {
                error = pipeline.Train(trainingData, classf);
                
                if (error < _toleranceThreshold || error == lastError) break;

                lastError = error;
            }

            return new Tuple<ClassificationPipeline<TClass, TInput, double>, double>(pipeline, error);
        }

        private static Config Setup(
            IFeatureExtractor<TInput, double> featureExtractor,
            IOutputMapper<TClass> outputMapper,
            TInput normalisingSample)
        {
            if (outputMapper == null) outputMapper = new OutputMapper<TClass>();
            
            return new Config()
            {
                NormalisingSample = normalisingSample,
                FeatureExtractor = featureExtractor,
                OutputMapper = outputMapper
            };
        }

        private class Config
        {
            public  TInput NormalisingSample { get; set; }
            public IOutputMapper<TClass> OutputMapper { get; set; }
            public IFeatureExtractor<TInput, double> FeatureExtractor { get; set; }
        }
    }
}