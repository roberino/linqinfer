using LinqInfer.Data.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    class TimeSequenceAsyncTrainingSet<T> : 
        AsyncPipe<TrainingPair<IVector, IVector>>, IAsyncTrainingSet<T, T>
        where T : IEquatable<T>
    {
        public TimeSequenceAsyncTrainingSet(IAsyncEnumerator<T> dataSource,
            CategoricalFeatureExtractor<T, T> featureExtractor) : this(new AsyncFeatureProcessingPipeline<T>(dataSource, featureExtractor), new OutputMapper<T>(featureExtractor.Encoder))
        {
        }

        public TimeSequenceAsyncTrainingSet(IAsyncFeatureProcessingPipeline<T> pipeline,
            ICategoricalOutputMapper<T> outputMapper) : base(ExtractBatches(pipeline))
        {
            FeaturePipeline = pipeline;
            OutputMapper = outputMapper;
        }

        public IAsyncFeatureProcessingPipeline<T> FeaturePipeline { get; }

        public ICategoricalOutputMapper<T> OutputMapper { get; }

        static IAsyncEnumerator<TrainingPair<IVector, IVector>> ExtractBatches(IAsyncFeatureProcessingPipeline<T> pipeline)
        {
            return pipeline
                .ExtractBatches()
                .TransformEachBatch(b =>
                {
                    if (b.Count == 0)
                    {
                        return new List<TrainingPair<IVector, IVector>>();
                    }

                    var last = b[0].Vector;

                    return b
                        .Skip(1)
                        .Select(x =>
                        {
                            var pair = new TrainingPair<IVector, IVector>(last, x.Vector);
                            last = x.Vector;
                            return pair;
                        })
                        .ToList();
                });
        }
    }
}