using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class MultilayerNetworkObjectClassifier<TClass, TInput> :
        INetworkClassifier<TClass, TInput> 
        where TClass : IEquatable<TClass>
    {
        protected readonly Config _config;

        protected IMultilayerNetwork _network;

        public MultilayerNetworkObjectClassifier(
            IVectorFeatureExtractor<TInput> featureExtractor,
            ICategoricalOutputMapper<TClass> outputMapper,
            IMultilayerNetwork network = null) : this(Setup(featureExtractor, outputMapper, default(TInput)))
        {
            Statistics = new ClassifierStats();

            _network = network;
        }

        MultilayerNetworkObjectClassifier(Config config)
        {
            Statistics = new ClassifierStats();

            _config = config;
        }

        public void Reset()
        {
            _network.Reset();
        }

        public ClassifierStats Statistics { get; private set; }

        public ISerialisableDataTransformation DataTransformation => _network;

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

        public static MultilayerNetworkObjectClassifier<TClass, TInput> Create(PortableDataDocument data) =>
            Create(data, FeatureExtractorFactory<TInput>.Default);

        public static MultilayerNetworkObjectClassifier<TClass, TInput> Create(PortableDataDocument data, IFeatureExtractorFactory<TInput> featureExtractorFactory)
        {
            var stats = new ClassifierStats();

            stats.ImportData(data.Children[0]);

            var featureExtractor = featureExtractorFactory.Create(data.Children[1]);

            var network = MultilayerNetwork.CreateFromData(data.Children[2]);
            var outputMapper = OutputMapper<TClass>.ImportData(data.Children[3]);

            var mlnoc = new MultilayerNetworkObjectClassifier<TClass, TInput>(featureExtractor, outputMapper, network)
            {
                Statistics = stats
            };

            return mlnoc;
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(TInput obj)
        {
            if (_network == null) throw new InvalidOperationException("Pipeline not trained");

            var input = _config.FeatureExtractor.ExtractIVector(obj);

            var output = _network.Apply(input);

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
            return _network == null ? "Un-initialised classifier" : "NN classifier" + _network;
        }

        public IDynamicClassifier<TClass, TInput> Clone(bool deep)
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

        static Config Setup(
            IVectorFeatureExtractor<TInput> featureExtractor,
            ICategoricalOutputMapper<TClass> outputMapper,
            TInput normalisingSample)
        {
            return new Config()
            {
                NormalisingSample = normalisingSample,
                FeatureExtractor = featureExtractor,
                OutputMapper = outputMapper
            };
        }

        protected class Config
        {
            public TInput NormalisingSample { get; set; }
            public ICategoricalOutputMapper<TClass> OutputMapper { get; set; }
            public IVectorFeatureExtractor<TInput> FeatureExtractor { get; set; }
        }
    }
}