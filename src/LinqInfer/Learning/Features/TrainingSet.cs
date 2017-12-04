using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;

namespace LinqInfer.Learning.Features
{
    internal class TrainingSet<TInput, TClass> : ITrainingSet<TInput, TClass>
        where TInput : class
        where TClass : IEquatable<TClass>
    {
        private readonly IFeatureProcessingPipeline<TInput> _pipeline;
        private readonly Expression<Func<TInput, TClass>> _classf;
        private readonly Lazy<ICategoricalOutputMapper<TClass>> _outputMapper;

        internal TrainingSet(IFeatureProcessingPipeline<TInput> pipeline, Expression<Func<TInput, TClass>> classf)
        {
            _pipeline = pipeline;
            _classf = classf;

            _outputMapper = new Lazy<ICategoricalOutputMapper<TClass>>(() => new OutputMapperFactory<TInput, TClass>().Create(pipeline.Data, classf));
        }

        public IFeatureProcessingPipeline<TInput> FeaturePipeline
        {
            get
            {
                return _pipeline;
            }
        }

        public ICategoricalOutputMapper<TClass> OutputMapper
        {
            get
            {
                return _outputMapper.Value;
            }
        }

        public Expression<Func<TInput, TClass>> ClassifyingExpression
        {
            get
            {
                return _classf;
            }
        }

        public IEnumerable<TrainingPair<TInput, TClass>> ExtractTrainingObjects()
        {
            var cf = _classf.Compile();

            return _pipeline.Data.Select(d => new TrainingPair<TInput, TClass>(d, cf(d)));
        }

        public IEnumerable<IList<TrainingPair<IVector, IVector>>> ExtractTrainingVectorBatches(int batchSize = 1000)
        {
            var cf = _classf.Compile();

            foreach (var batch in _pipeline.ExtractBatches(batchSize))
            {
                yield return batch.Select(b => new TrainingPair<IVector, IVector>(b.VirtualVector, _outputMapper.Value.ExtractIVector(cf(b.Value)))).ToList();
            }
        }

        public IQueryable<IGrouping<TClass, ObjectVector<TInput>>> GetEnumerator()
        {
            return _pipeline
                .Data
                .GroupBy(_classf)
                .Select(g =>
                    (IGrouping<TClass, ObjectVector<TInput>>)new G(g.Key,
                        g.Select(x =>
                            new ObjectVector<TInput>(x, _pipeline.FeatureExtractor.ExtractIVector(x))))
                            );
        }

        private class G : IGrouping<TClass, ObjectVector<TInput>>
        {
            private readonly IEnumerable<ObjectVector<TInput>> _data;

            public G(TClass key, IEnumerable<ObjectVector<TInput>> data)
            {
                Key = key;
                _data = data;
            }

            public TClass Key { get; private set; }

            public IEnumerator<ObjectVector<TInput>> GetEnumerator()
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