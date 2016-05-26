using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkObjectClassifier<TClass, TInput> : IObjectClassifier<TClass, TInput> where TClass : IEquatable<TClass>
    {
        protected readonly Config _config;

        protected IObjectClassifier<TClass, TInput> _classifier;

        public MultilayerNetworkObjectClassifier(
            IFeatureExtractor<TInput, double> featureExtractor,
            ICategoricalOutputMapper<TClass> outputMapper = null) : this(Setup(featureExtractor, outputMapper, default(TInput)))
        {
        }

        private MultilayerNetworkObjectClassifier(Config config)
        {
            _config = config;
        }

        public void Load(Stream input)
        {
            _config.OutputMapper.Load(input);
            _config.FeatureExtractor.Load(input);

            var network = new MultilayerNetwork(input);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, _config.OutputMapper.Map);

            _classifier = new ObjectClassifier<TClass, TInput, double>(classifier, _config.FeatureExtractor);
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(TInput obj)
        {
            if (_classifier == null) throw new InvalidOperationException("Pipeline not trained");

            return _classifier.Classify(obj);
        }

        private static Config Setup(
            IFeatureExtractor<TInput, double> featureExtractor,
            ICategoricalOutputMapper<TClass> outputMapper,
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

        protected class Config
        {
            public  TInput NormalisingSample { get; set; }
            public ICategoricalOutputMapper<TClass> OutputMapper { get; set; }
            public IFeatureExtractor<TInput, double> FeatureExtractor { get; set; }
        }
    }
}