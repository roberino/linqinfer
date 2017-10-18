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
        FeatureProcessingPipeline<TInput> FeaturePipeline { get; }
        ICategoricalOutputMapper<TClass> OutputMapper { get; }

        IEnumerable<IList<ObjectVector<TClass>>> ExtractInputClassBatches(int batchSize = 1000);
        IEnumerable<IList<Tuple<ColumnVector1D, ColumnVector1D>>> ExtractInputOutputVectorBatches(int batchSize = 1000);
        IEnumerable<IList<TrainingPair<IVector, IVector>>> ExtractInputOutputIVectorBatches(int batchSize = 1000);
    }
}