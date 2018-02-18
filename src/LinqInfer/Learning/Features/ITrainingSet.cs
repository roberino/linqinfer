using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    public interface ITrainingSet<TInput, TClass>
        where TInput : class
        where TClass : IEquatable<TClass>
    {
        Expression<Func<TInput, TClass>> ClassifyingExpression { get; }
        IFeatureProcessingPipeline<TInput> FeaturePipeline { get; }
        ICategoricalOutputMapper<TClass> OutputMapper { get; }

        IEnumerable<TrainingPair<TInput, TClass>> ExtractTrainingObjects();
        IEnumerable<IList<TrainingPair<IVector, IVector>>> ExtractTrainingVectorBatches(int batchSize = 1000);
    }
}