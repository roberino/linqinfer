using LinqInfer.Data.Pipes;
using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Features
{
    public interface IAsyncTrainingSet<TInput, TClass> : 
        IAsyncPipe<TrainingPair<IVector, IVector>>
        where TClass : IEquatable<TClass>
    {
        IAsyncFeatureProcessingPipeline<TInput> FeaturePipeline { get; }

        ICategoricalOutputMapper<TClass> OutputMapper { get; }
    }
}