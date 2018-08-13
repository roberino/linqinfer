using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;

namespace LinqInfer.Learning.Features
{
    class TrainingSet<TInput, TClass> : ITrainingSet<TInput, TClass>
        where TInput : class
        where TClass : IEquatable<TClass>
    {
        readonly Lazy<ICategoricalOutputMapper<TClass>> _outputMapper;

        internal TrainingSet(IFeatureProcessingPipeline<TInput> pipeline, Expression<Func<TInput, TClass>> classf)
        {
            FeaturePipeline = pipeline;
            ClassifyingExpression = classf;

            _outputMapper = new Lazy<ICategoricalOutputMapper<TClass>>(() => new OutputMapperFactory<TInput, TClass>().Create(pipeline.Data, classf));
        }

        public IFeatureProcessingPipeline<TInput> FeaturePipeline { get; }

        public ICategoricalOutputMapper<TClass> OutputMapper => _outputMapper.Value;

        public Expression<Func<TInput, TClass>> ClassifyingExpression { get; }

        public IEnumerable<TrainingPair<TInput, TClass>> ExtractTrainingObjects()
        {
            var cf = ClassifyingExpression.Compile();

            return FeaturePipeline.Data.Select(d => new TrainingPair<TInput, TClass>(d, cf(d)));
        }

        public IEnumerable<IList<TrainingPair<IVector, IVector>>> ExtractTrainingVectorBatches(int batchSize = 1000)
        {
            var cf = ClassifyingExpression.Compile();

            foreach (var batch in FeaturePipeline.ExtractBatches(batchSize))
            {
                yield return batch.Select(b => new TrainingPair<IVector, IVector>(b.Vector, _outputMapper.Value.ExtractIVector(cf(b.Value)))).ToList();
            }
        }

        public IQueryable<IGrouping<TClass, ObjectVectorPair<TInput>>> GetEnumerator()
        {
            return FeaturePipeline
                .Data
                .GroupBy(ClassifyingExpression)
                .Select(g =>
                    (IGrouping<TClass, ObjectVectorPair<TInput>>)new G(g.Key,
                        g.Select(x =>
                            new ObjectVectorPair<TInput>(x, FeaturePipeline.FeatureExtractor.ExtractIVector(x))))
                            );
        }

        class G : IGrouping<TClass, ObjectVectorPair<TInput>>
        {
            readonly IEnumerable<ObjectVectorPair<TInput>> _data;

            public G(TClass key, IEnumerable<ObjectVectorPair<TInput>> data)
            {
                Key = key;
                _data = data;
            }

            public TClass Key { get; private set; }

            public IEnumerator<ObjectVectorPair<TInput>> GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _data.GetEnumerator();
            }
        }
    }
}