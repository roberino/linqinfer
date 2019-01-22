using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using System;
using System.Linq.Expressions;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;

namespace LinqInfer.Learning
{
    public class NetworkFactory<TInput>
    {
        readonly IFloatingPointFeatureExtractor<TInput> _featureExtractor;

        NetworkFactory(IFloatingPointFeatureExtractor<TInput> featureExtractor)
        {
            _featureExtractor = featureExtractor;
        }

        public static NetworkFactory<TInput> CreateNetworkFactory(IFloatingPointFeatureExtractor<TInput> featureExtractor)
        {
            return new NetworkFactory<TInput>(featureExtractor);
        }

        public static NetworkFactory<TInput> CreateNetworkFactory()
        {
            var fe = new ObjectFeatureExtractor<TInput>();

            return new NetworkFactory<TInput>(fe);
        }

        public static NetworkFactory<TInput> CreateNetworkFactoryFromExpression(Expression<Func<TInput, IVector>> vectorExpression, int vectorSize)
        {
            var fe = new ExpressionFeatureExtractor<TInput>(vectorExpression, vectorSize);

            return new NetworkFactory<TInput>(fe);
        }

        public static NetworkFactory<TInput> CreateCategoricalNetworkFactory(int maxCategories)
        {
            var fe = new CategoricalFeatureExtractor<TInput, TInput>(x => x, maxCategories);

            return new NetworkFactory<TInput>(fe);
        }

        public INetworkClassifier<TClass, TInput> CreateLongShortTermMemoryNetwork<TClass>(
            int maxOutputs)
            where TClass : IEquatable<TClass>
        {
            var outputMapper = new OutputMapper<TClass>(new OneHotEncoding<TClass>(maxOutputs));

            return BuildLongShortTermMemoryNetwork(outputMapper).classifier;
        }

        internal (INetworkClassifier<TClass, TInput> classifier, IClassifierTrainingContext<INetworkModel> trainer) BuildLongShortTermMemoryNetwork<TClass>(
            ICategoricalOutputMapper<TClass> outputMapper)
            where TClass : IEquatable<TClass>
        {
            var builder = RecurrentNetworkBuilder
                .Create(_featureExtractor.VectorSize)
                .ConfigureLongShortTermMemoryNetwork(outputMapper.VectorSize);

            var trainingContext = builder.Build();

            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(_featureExtractor, outputMapper,
                (MultilayerNetwork)trainingContext.Output);

            return (classifier, trainingContext);
        }
    }
}