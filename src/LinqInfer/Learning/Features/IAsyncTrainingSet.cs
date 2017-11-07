using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqInfer.Maths;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    public interface IAsyncTrainingSet<TInput, TClass>
        where TInput : class
        where TClass : IEquatable<TClass>
    {
        Expression<Func<TInput, TClass>> ClassifyingExpression { get; }
        IFeatureProcessingPipeline<TInput> FeaturePipeline { get; }
        ICategoricalOutputMapper<TClass> OutputMapper { get; }

        IEnumerable<Task<IList<TrainingPair<IVector, IVector>>>> ExtractInputOutputIVectorBatches(int batchSize = 1000);
    }
}