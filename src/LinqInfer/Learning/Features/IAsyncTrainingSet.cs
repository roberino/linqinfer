﻿using LinqInfer.Data.Pipes;
using LinqInfer.Maths;
using System;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    public interface IAsyncTrainingSet<TInput, TClass> : IAsyncPipe<TrainingPair<IVector, IVector>>
        where TClass : IEquatable<TClass>
    {
        Expression<Func<TInput, TClass>> ClassifyingExpression { get; }
        IAsyncFeatureProcessingPipeline<TInput> FeaturePipeline { get; }
        ICategoricalOutputMapper<TClass> OutputMapper { get; }

        IAsyncEnumerator<TrainingPair<IVector, IVector>> ExtractInputOutputIVectorBatches(int batchSize = 1000);
    }
}