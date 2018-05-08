using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths.Graphs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MultilayerNetworkObjectClassifier<TClass, TInput> :
        INetworkClassifier<TClass, TInput>
        where TClass : IEquatable<TClass>
    {
        protected readonly Config _config;

        protected MultilayerNetwork _network;

        public MultilayerNetworkObjectClassifier(
            IFloatingPointFeatureExtractor<TInput> featureExtractor,
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

        public ISerialisableDataTransformation DataTransformation => _network;

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

        public PortableDataDocument ExportData()
        {
            if (_network == null)
            {
                throw new InvalidOperationException("No training data received");
            }

            var root = new PortableDataDocument();

            root.WriteChildObject(Statistics);
            root.WriteChildObject(_config.FeatureExtractor);
            root.WriteChildObject(_network);
            root.WriteChildObject(_config.OutputMapper);

            return root;
        }

        public void ImportData(PortableDataDocument doc)
        {
            doc.ReadChildObject(Statistics, null, true);

            doc.ReadChildObject(_config.FeatureExtractor);

            if (_network == null)
            {
                _network = MultilayerNetwork.CreateFromVectorDocument(doc.GetChildDoc<MultilayerNetwork>());
            }
            else
            {
                _network.ImportData(doc.GetChildDoc<MultilayerNetwork>());
            }

            doc.ReadChildObject(_config.OutputMapper);

            Setup(_network);
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(TInput obj)
        {
            if (_network == null) throw new InvalidOperationException("Pipeline not trained");

            var input = _config.FeatureExtractor.ExtractIVector(obj);

            var output = _network.Evaluate(input);

            var outputObjects = _config.OutputMapper.Map(output);

            Statistics.IncrementClassificationCount();
            
            return outputObjects;
        }

        public void Train(TInput obj, TClass classification)
        {
            if (_network == null) throw new InvalidOperationException("Pipeline not initialised");

            var inputVector = _config.FeatureExtractor.ExtractIVector(obj);
            var targetVector = _config.OutputMapper.ExtractIVector(classification);

            new BackPropagationLearning(_network)
                .Train(inputVector, targetVector);

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

            classifier._network = newNn;
            classifier.Statistics = Statistics.Clone(true);

            return classifier;
        }

        public async Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null)
        {
            var graph = await _network.ExportNetworkTopologyAsync(visualSettings, store);

            return graph;
        }

        public object Clone()
        {
            return Clone(true);
        }

        private static Config Setup(
            IFloatingPointFeatureExtractor<TInput> featureExtractor,
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
            _network = network;
        }

        protected class Config
        {
            public TInput NormalisingSample { get; set; }
            public ICategoricalOutputMapper<TClass> OutputMapper { get; set; }
            public IFloatingPointFeatureExtractor<TInput> FeatureExtractor { get; set; }
        }
    }
}