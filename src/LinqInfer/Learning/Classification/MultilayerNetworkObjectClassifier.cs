using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkObjectClassifier<TClass, TInput> : 
        IDynamicClassifier<TClass, TInput>
        where TClass : IEquatable<TClass>
    {
        protected readonly Config _config;

        protected MultilayerNetwork _network;
        protected IObjectClassifier<TClass, TInput> _classifier;

        public MultilayerNetworkObjectClassifier(
            IFeatureExtractor<TInput, double> featureExtractor,
            ICategoricalOutputMapper<TClass> outputMapper = null,
            MultilayerNetwork network = null) : this(Setup(featureExtractor, outputMapper, default(TInput)))
        {
            Statistics = new ClassifierStats();

            if (network != null)
            {
                Setup(network);
            }
        }

        private MultilayerNetworkObjectClassifier(Config config)
        {
            Statistics = new ClassifierStats();

            _config = config;
        }

        public ClassifierStats Statistics { get; private set; }

        public void Load(Stream input)
        {
            _config.OutputMapper.Load(input);
            _config.FeatureExtractor.Load(input);

            var network = new MultilayerNetwork(input);

            Setup(network);
        }

        public virtual void Save(Stream output)
        {
            if (_network == null)
            {
                throw new InvalidOperationException("No training data received");
            }

            _config.OutputMapper.Save(output);
            _config.FeatureExtractor.Save(output);
            _network.Save(output);
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            if (_network == null)
            {
                throw new InvalidOperationException("No training data received");
            }

            var root = new BinaryVectorDocument();

            root.WriteChildObject(Statistics);
            root.WriteChildObject(_config.FeatureExtractor);
            root.WriteChildObject(_network);
            root.WriteChildObject(_config.OutputMapper);

            return root;
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            doc.ReadChildObject(Statistics, null, true);

            doc.ReadChildObject(_config.FeatureExtractor);

            if (_network == null)
            {
                _network = MultilayerNetwork.CreateFromVectorDocument(doc.GetChildDoc<MultilayerNetwork>());
            }
            else
            {
                _network.FromVectorDocument(doc.GetChildDoc<MultilayerNetwork>());
            }

            doc.ReadChildObject(_config.OutputMapper);

            Setup(_network);
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(TInput obj)
        {
            if (_classifier == null) throw new InvalidOperationException("Pipeline not trained");

            var results = _classifier.Classify(obj);

            Statistics.IncrementClassificationCount();
            
            return results;
        }

        public void Train(TInput obj, TClass classification)
        {
            if (_classifier == null) throw new InvalidOperationException("Pipeline not initialised");

            new BackPropagationLearning(_network)
                .Train(new ColumnVector1D(_config.FeatureExtractor.ExtractVector(obj)), new ColumnVector1D(_config.OutputMapper.ExtractVector(classification)));

            Statistics.IncrementTrainingSampleCount();
        }

        public override string ToString()
        {
            return _network == null ? "Un-initialised classifier" : "NN classifier" + _network.ToString();
        }

        public void PruneFeatures(params int[] featureIndexes)
        {
            _network.PruneInputs(featureIndexes);
        }

        public IPrunableObjectClassifier<TClass, TInput> Clone(bool deep)
        {
            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(_config);

            var newNn = _network.Clone(true);

            var mnClassifier = new MultilayerNetworkClassifier<TClass>(_config.OutputMapper, _network);

            classifier._network = newNn;
            classifier._classifier = new ObjectClassifier<TClass, TInput, double>(mnClassifier, _config.FeatureExtractor);
            classifier.Statistics = Statistics.Clone(true);

            return classifier;
        }

        public object Clone()
        {
            return Clone(true);
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

        private void Setup(MultilayerNetwork network)
        {
            var classifier = new MultilayerNetworkClassifier<TClass>(_config.OutputMapper, network);

            _network = network;
            _classifier = new ObjectClassifier<TClass, TInput, double>(classifier, _config.FeatureExtractor);
        }

        protected class Config
        {
            public TInput NormalisingSample { get; set; }
            public ICategoricalOutputMapper<TClass> OutputMapper { get; set; }
            public IFeatureExtractor<TInput, double> FeatureExtractor { get; set; }
        }
    }
}