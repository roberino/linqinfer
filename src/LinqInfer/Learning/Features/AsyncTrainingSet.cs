using LinqInfer.Data.Pipes;
using LinqInfer.Maths;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    class AsyncTrainingSet<TInput, TClass> : AsyncPipe<TrainingPair<IVector, IVector>>, IAsyncTrainingSet<TInput, TClass>
        where TClass : IEquatable<TClass>
    {
        internal AsyncTrainingSet(
            IAsyncFeatureProcessingPipeline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            ICategoricalOutputMapper<TClass> outputMapper)
            : base(ExtractBatches(pipeline, outputMapper, classf?.Compile()))
        {
            FeaturePipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            ClassifyingExpression = classf ?? throw new ArgumentNullException(nameof(classf));
            OutputMapper = outputMapper ?? throw new ArgumentNullException(nameof(outputMapper));
        }

        public Expression<Func<TInput, TClass>> ClassifyingExpression { get; }

        public IAsyncFeatureProcessingPipeline<TInput> FeaturePipeline { get; }

        public ICategoricalOutputMapper<TClass> OutputMapper { get; }

        public ITransformingAsyncBatchSource<TrainingPair<IVector, IVector>> ExtractInputOutputIVectorBatches(int batchSize = 1000)
        {
            var clsFunc = ClassifyingExpression.Compile();

            return ExtractBatches(FeaturePipeline, OutputMapper, clsFunc);
        }

        static ITransformingAsyncBatchSource<TrainingPair<IVector, IVector>> ExtractBatches(IAsyncFeatureProcessingPipeline<TInput> pipeline, ICategoricalOutputMapper<TClass> outputMapper, Func<TInput, TClass> classifyingFunc)
        {
            return pipeline
                   .ExtractBatches()
                   .TransformEachBatch(b => b
                           .Select(x => new TrainingPair<IVector, IVector>(x.Vector, outputMapper.ExtractIVector(classifyingFunc(x.Value))))
                           .ToList());
        }
    }
}