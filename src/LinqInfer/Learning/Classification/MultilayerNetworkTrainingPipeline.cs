using LinqInfer.Data;
using LinqInfer.Learning.Features;
using System;
using System.Linq.Expressions;
using System.IO;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkTrainingPipeline<TClass, TInput> where TInput : class where TClass : IEquatable<TClass>
    {
        private readonly ICategoricalOutputMapper<TClass> _outputMapper;
        private readonly IFeatureProcessingPipeline<TInput> _featureData;
        private readonly Expression<Func<TInput, TClass>> _classifyingExpression;

        private IClassifierTrainingContext<TClass, NetworkParameters> _result;

        public MultilayerNetworkTrainingPipeline(IFeatureProcessingPipeline<TInput> featureData, Expression<Func<TInput, TClass>> classifyingExpression)
        {
            _featureData = featureData;
            _classifyingExpression = classifyingExpression;
            _outputMapper = new OutputMapperFactory<TInput, TClass>().Create(featureData.Data, classifyingExpression);
        }

        public IPrunableObjectClassifier<TClass, TInput> TrainUsing(IMultilayerNetworkTrainingStrategy<TClass, TInput> trainingStrategy)
        {
            var trainingContextFactory = new MultilayerNetworkTrainingContextFactory<TClass>(_outputMapper);

            _result = trainingStrategy.Train(_featureData, trainingContextFactory.Create, _classifyingExpression, _outputMapper);

            return new MultilayerNetworkObjectClassifier<TClass, TInput>(_featureData.FeatureExtractor, _outputMapper, ((MultilayerNetworkClassifier<TClass>)_result.Classifier).Network);
        }
    }
}