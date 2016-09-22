using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    internal class TrainingSet<TInput, TClass> : ITrainingSet<TInput, TClass>
        where TInput : class
        where TClass : IEquatable<TClass>
    {
        private readonly FeatureProcessingPipline<TInput> _pipeline;
        private readonly Expression<Func<TInput, TClass>> _classf;
        private readonly Lazy<ICategoricalOutputMapper<TClass>> _outputMapper;

        internal TrainingSet(FeatureProcessingPipline<TInput> pipeline, Expression<Func<TInput, TClass>> classf)
        {
            _pipeline = pipeline;
            _classf = classf;

            _outputMapper = new Lazy<ICategoricalOutputMapper<TClass>>(() => new OutputMapperFactory<TInput, TClass>().Create(pipeline.Data, classf));
        }

        public FeatureProcessingPipline<TInput> FeaturePipeline
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

        public IEnumerable<IList<ObjectVector<TClass>>> ExtractInputClassBatches(int batchSize = 1000)
        {
            var cf = _classf.Compile();

            foreach (var batch in _pipeline.ExtractBatches(batchSize))
            {
                yield return batch.Select(b => new ObjectVector<TClass>(cf(b.Value), b.Vector)).ToList();
            }
        }

        public IEnumerable<IList<Tuple<ColumnVector1D, ColumnVector1D>>> ExtractInputOutputVectorBatches(int batchSize = 1000)
        {
            var cf = _classf.Compile();

            foreach (var batch in _pipeline.ExtractBatches(batchSize))
            {
                yield return batch.Select(b => new Tuple<ColumnVector1D, ColumnVector1D>(b.Vector, _outputMapper.Value.ExtractColumnVector(cf(b.Value)))).ToList();
            }
        }
    }
}