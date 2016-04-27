using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetworkClassificationPipeline<TClass, TInput> where TClass : IEquatable<TClass>
    {
        private readonly Config _config;

        private ClassificationPipeline<TClass, TInput, double> _pipeline;

        public MultilayerNetworkClassificationPipeline(
            IFeatureExtractor<TInput, double> featureExtractor,
            ActivatorFunc activator = null,
            IOutputMapper<TClass> outputMapper = null) : this(Setup(featureExtractor, outputMapper, default(TInput)))
        {
            ErrorTolerance = 0.3f;
        }

        private MultilayerNetworkClassificationPipeline(Config config) // : base(config.Trainer, config.Classifier, config.FeatureExtractor, config.NormalisingSample)
        {
            _config = config;
        }

        public float ErrorTolerance { get; set; }

        public double Train(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classifyingExpression)
        {
            int hiddenLayerFactor = 8;
            IList<TClass> outputs = null;

            if (_config.OutputMapper.VectorSize == 0)
            {
                outputs = trainingData.GroupBy(classifyingExpression).Select(o => o.Key).ToList();

                _config.OutputMapper.Initialise(outputs);
                _config.FeatureExtractor.NormaliseUsing(trainingData);
            }

            var networks = Enumerable.Range(0, hiddenLayerFactor).Select(n => SetupNetwork(n, outputs.Count)).ToList();

            var iterationReductionFactor = hiddenLayerFactor; // reduce by 1/8 each iteration
            var i = 0;
            var classf = classifyingExpression.Compile();
            var nc = networks.Count;
            var trainingDataCount = trainingData.Count();

            while (i < 1000)
            {
                var unconverged = networks.Where(n => n.Error / trainingDataCount < ErrorTolerance).ToList();

                unconverged.AsParallel().ForAll(n => n.ResetError());

                foreach (var batch in trainingData.Chunk())
                {
                    unconverged.AsParallel().WithDegreeOfParallelism(1).ForAll(n =>
                    {
                        foreach(var value in batch)
                        {
                            n.Train(value, classf);
                        }
                    });
                }

                if (networks.All(n => n.Error / trainingDataCount < ErrorTolerance)) break;

                nc = networks.Count * (iterationReductionFactor - 1) / iterationReductionFactor;

                if (networks.Count > 2) networks = networks.OrderBy(n => n.Error).Take(nc).ToList();

                i++;
            }

            _pipeline = networks.OrderBy(n => n.Error).First();

            Debugger.Log(_pipeline);

            return _pipeline.Error / trainingDataCount;
        }

        public ClassifyResult<TClass> Classify(TInput obj)
        {
            if (_pipeline == null) throw new InvalidOperationException("Pipeline not trained");

            return _pipeline.Classify(obj);
        }

        private ClassificationPipeline<TClass, TInput, double> SetupNetwork(int hiddenLayerFactor, int outputSize)
        {
            var network = new MultilayerNetwork(_config.FeatureExtractor.VectorSize);
            var bpa = new BackPropagationLearning(network);
            var learningAdapter = new AssistedLearningAdapter<TClass>(bpa, _config.OutputMapper);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, _config.OutputMapper.Map);

            var pipeline = new ClassificationPipeline<TClass, TInput, double>(learningAdapter, classifier, _config.FeatureExtractor, _config.NormalisingSample, false);

            network.Initialise(_config.FeatureExtractor.VectorSize, _config.FeatureExtractor.VectorSize / 2 * hiddenLayerFactor, outputSize);
            
            return pipeline;
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