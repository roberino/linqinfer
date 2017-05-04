﻿using LinqInfer.Learning.Features;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkTrainingRunner<TClass, TInput> where TInput : class where TClass : IEquatable<TClass>
    {
        private readonly ITrainingSet<TInput, TClass> _trainingSet;

        private IClassifierTrainingContext<TClass, NetworkParameters> _result;

        public MultilayerNetworkTrainingRunner(ITrainingSet<TInput, TClass> trainingSet)
        {
            _trainingSet = trainingSet;
        }

        public ICategoricalOutputMapper<TClass> OutputMapper
        {
            get
            {
                return _trainingSet.OutputMapper;
            }
        }

        public async Task<IDynamicClassifier<TClass, TInput>> TrainUsing(IAsyncMultilayerNetworkTrainingStrategy<TClass, TInput> trainingStrategy)
        {
            var trainingContextFactory = new MultilayerNetworkTrainingContextFactory<TClass>(_trainingSet.OutputMapper);

            _result = await trainingStrategy.Train(_trainingSet, trainingContextFactory.Create);

            return new MultilayerNetworkObjectClassifier<TClass, TInput>(_trainingSet.FeaturePipeline.FeatureExtractor, _trainingSet.OutputMapper, ((MultilayerNetworkClassifier<TClass>)_result.Classifier).Network);
        }
    }
}