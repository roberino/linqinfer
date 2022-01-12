using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Linq.Expressions;

namespace LinqInfer.Learning
{
    public static class NetworkFactory
    {
        static INetworkFactory<TInput> CreateNetworkFactory<TInput>(IVectorFeatureExtractor<TInput> featureExtractor)
        {
            return new NetworkFactoryImpl<TInput>(featureExtractor);
        }

        public static INetworkFactory<TInput> CreateNetworkFactory<TInput>() =>
            CreateNetworkFactory(new ObjectFeatureExtractor<TInput>());

        public static INetworkFactory<TInput> CreateNetworkFactory<TInput>(
            Expression<Func<TInput, IVector>> vectorExpression, int vectorSize)
            => CreateNetworkFactory(new ExpressionFeatureExtractor<TInput>(vectorExpression, vectorSize));

        public static ICategoricalNetworkFactory<TInput> CreateCategoricalNetworkFactory<TInput>(int maxCategories)
            where TInput : IEquatable<TInput>
        {
            var fe = new CategoricalFeatureExtractor<TInput, TInput>(x => x, maxCategories);

            return new CategoricalNetworkFactoryImpl<TInput>(fe);
        }

        class NetworkFactoryImpl<TInput> : INetworkFactory<TInput>
        {
            protected readonly IVectorFeatureExtractor<TInput> FeatureExtractor;

            public NetworkFactoryImpl(IVectorFeatureExtractor<TInput> featureExtractor)
            {
                FeatureExtractor = featureExtractor;
            }

            public INetworkClassifier<TClass, TInput> CreateConvolutionalNetwork<TClass>(
                int maxOutputs, 
                int? hiddenLayerSize = null, 
                Action<LearningParameters> learningConfig = null) where TClass : IEquatable<TClass>
            {
                var outputMapper = new OutputMapper<TClass>(new OneHotEncoding<TClass>(maxOutputs));

                var builder = ConvolutionalNetworkBuilder
                    .Create(FeatureExtractor.VectorSize)
                    .ConfigureSoftmaxNetwork(hiddenLayerSize.GetValueOrDefault(maxOutputs * 2), learningConfig);

                var trainingContext = builder.Build();

                var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(FeatureExtractor, outputMapper,
                    (MultilayerNetwork)trainingContext.Output);

                return classifier;
            }
        }

        class CategoricalNetworkFactoryImpl<TInput> : NetworkFactoryImpl<TInput>, ICategoricalNetworkFactory<TInput>
            where TInput : IEquatable<TInput>
        {
            public CategoricalNetworkFactoryImpl(IVectorFeatureExtractor<TInput> featureExtractor) : base(featureExtractor)
            {
            }

            public ITimeSequenceAnalyser<TInput> CreateTimeSequenceAnalyser()
            {
                var outputMapper = new OutputMapper<TInput>(new OneHotEncoding<TInput>(FeatureExtractor.VectorSize));

                return new TimeSequenceAnalyser<TInput>(BuildLongShortTermMemoryNetwork(outputMapper).classifier);
            }

            public ITimeSequenceAnalyser<TInput> CreateTimeSequenceAnalyser(PortableDataDocument data)
            {
                return TimeSequenceAnalyser<TInput>.Create(data);
            }

            (INetworkClassifier<TInput, TInput> classifier, IClassifierTrainingContext<INetworkModel> trainer)
                BuildLongShortTermMemoryNetwork(ICategoricalOutputMapper<TInput> outputMapper)
            {
                var builder = RecurrentNetworkBuilder
                    .Create(FeatureExtractor.VectorSize)
                    .ConfigureLongShortTermMemoryNetwork(outputMapper.VectorSize);

                var trainingContext = builder.Build();

                var classifier = new MultilayerNetworkObjectClassifier<TInput, TInput>(FeatureExtractor, outputMapper,
                    (MultilayerNetwork) trainingContext.Output);

                return (classifier, trainingContext);
            }
        }
    }
}