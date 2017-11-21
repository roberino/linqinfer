using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    internal class AsyncTrainingSet<TInput, TClass> : IAsyncTrainingSet<TInput, TClass>
        where TInput : class
        where TClass : IEquatable<TClass>
    {
        internal AsyncTrainingSet(
            IAsyncFeatureProcessingPipeline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            ICategoricalOutputMapper<TClass> outputMapper)
        {
            FeaturePipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            ClassifyingExpression = classf ?? throw new ArgumentNullException(nameof(classf));
            OutputMapper = outputMapper ?? throw new ArgumentNullException(nameof(outputMapper));
        }

        public Expression<Func<TInput, TClass>> ClassifyingExpression { get; }

        public IAsyncFeatureProcessingPipeline<TInput> FeaturePipeline { get; }

        public ICategoricalOutputMapper<TClass> OutputMapper { get; }

        public IAsyncEnumerator<TrainingPair<IVector, IVector>> ExtractInputOutputIVectorBatches(int batchSize = 1000)
        {
            var clsFunc = ClassifyingExpression.Compile();

            return FeaturePipeline
                .ExtractBatches()
                .TransformEachBatch(b => b
                        .Select(x => new TrainingPair<IVector, IVector>(x.VirtualVector, OutputMapper.ExtractIVector(clsFunc(x.Value))))
                        .ToList());
        }
    }
}