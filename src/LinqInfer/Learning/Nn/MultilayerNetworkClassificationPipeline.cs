using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Utility;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetworkClassificationPipeline<TClass, TInput> where TClass : IEquatable<TClass>
    {
        private readonly Config _config;

        private Pipeline _pipeline;

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
                var unconverged = networks.Where(n => n.Instance.Error / trainingDataCount < ErrorTolerance).ToList();

                unconverged.AsParallel().ForAll(n => n.Instance.ResetError());

                foreach (var batch in trainingData.Chunk())
                {
                    unconverged.AsParallel().WithDegreeOfParallelism(1).ForAll(n =>
                    {
                        foreach (var value in batch)
                        {
                            n.Instance.Train(value, classf);
                        }
                    });
                }

                if (networks.All(n => n.Instance.Error / trainingDataCount < ErrorTolerance)) break;

                nc = networks.Count * (iterationReductionFactor - 1) / iterationReductionFactor;

                if (networks.Count > 2) networks = networks.OrderBy(n => n.Instance.Error).Take(nc).ToList();

                networks.Add(Breed(networks[0], networks[1]));

                i++;
            }

            _pipeline = networks.OrderBy(n => n.Instance.Error).First();

            Debugger.Log(_pipeline);

            return _pipeline.Instance.Error / trainingDataCount;
        }

        public ClassifyResult<TClass> Classify(TInput obj)
        {
            if (_pipeline == null) throw new InvalidOperationException("Pipeline not trained");

            return _pipeline.Instance.Classify(obj);
        }

        private Pipeline Breed(Pipeline a, Pipeline b)
        {
            var newParams = a.Parameters.Breed(b.Parameters);

            var network = new MultilayerNetwork(newParams);
            var bpa = new BackPropagationLearning(network);
            var learningAdapter = new AssistedLearningAdapter<TClass>(bpa, _config.OutputMapper);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, _config.OutputMapper.Map);

            var pipeline = new ClassificationPipeline<TClass, TInput, double>(learningAdapter, classifier, _config.FeatureExtractor, _config.NormalisingSample, false);
            
            return new Pipeline()
            {
                Instance = pipeline,
                Parameters = network.Parameters
            };
        }

        private Pipeline SetupNetwork(int hiddenLayerFactor, int outputSize)
        {
            var activator = Functions.AorB(Activators.Sigmoid(), Activators.Threshold(), 0.7);
            var network = new MultilayerNetwork(_config.FeatureExtractor.VectorSize, activator);
            var bpa = new BackPropagationLearning(network);
            var learningAdapter = new AssistedLearningAdapter<TClass>(bpa, _config.OutputMapper);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, _config.OutputMapper.Map);

            var pipeline = new ClassificationPipeline<TClass, TInput, double>(learningAdapter, classifier, _config.FeatureExtractor, _config.NormalisingSample, false);

            network.Initialise(_config.FeatureExtractor.VectorSize, _config.FeatureExtractor.VectorSize / 2 * hiddenLayerFactor, outputSize);

            return new Pipeline()
            {
                Instance = pipeline,
                Parameters = network.Parameters
            };
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

        private class Pipeline
        {
            public ClassificationPipeline<TClass, TInput, double> Instance { get; set; }

            public NetworkParameters Parameters { get; set; }

            public override string ToString()
            {
                return Parameters.ToString();
            }
        }

        private class Config
        {
            public  TInput NormalisingSample { get; set; }
            public IOutputMapper<TClass> OutputMapper { get; set; }
            public IFeatureExtractor<TInput, double> FeatureExtractor { get; set; }
        }
    }
}