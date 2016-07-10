using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    public sealed class ExecutionPipline<TResult>
    {
        private const int _errorThreshold = 15;
        private readonly Func<string, TResult> _execute;
        private readonly Func<bool, TResult, bool> _feedback;
        private readonly IList<Exception> _errors;

        internal ExecutionPipline(Func<string, TResult> execute, Func<bool, TResult, bool> feedback)
        {
            _execute = execute;
            _feedback = feedback;
            _errors = new List<Exception>();
        }

        public TResult Execute(string outputName = null)
        {
            try
            {
                var res = _execute(outputName);

                _feedback(true, res);

                return res;
            }
            catch (Exception ex)
            {
                _errors.Add(ex);
                throw;
            }
        }

        public TResult ExecuteUntil(Func<TResult, bool> condition, string outputName = null)
        {
            int errorStart = _errors.Count;

            while (true)
            {
                var next = _execute(outputName);

                try
                {
                    if (condition(next))
                    {
                        if (_feedback(true, next))
                        {
                            return next;
                        }
                    }
                    else
                    {
                        _feedback(false, next);
                    }
                }
                catch (Exception ex)
                {
                    _errors.Add(ex);

                    if ((_errors.Count - errorStart) > _errorThreshold)
                    {
                        throw new AggregateException(_errors.Skip(errorStart));
                    }
                }
            }
        }

        public IEnumerable<Exception> Errors
        {
            get
            {
                return _errors;
            }
        }
    }

    public sealed class FeatureProcessingPipline<T> : IFeatureProcessingPipeline<T> where T : class
    {
        private static readonly ObjectFeatureExtractor _objExtractor = new ObjectFeatureExtractor();

        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly IQueryable<T> _data;
        private readonly IList<IBlobStore> _outputs;

        private FloatingPointTransformingFeatureExtractor<T> _transformation;

        internal FeatureProcessingPipline(IQueryable<T> data, IFloatingPointFeatureExtractor<T> featureExtractor = null)
        {
            _data = data;
            _featureExtractor = featureExtractor ?? _objExtractor.CreateFeatureExtractor<T>();
            _outputs = new List<IBlobStore>();
        }

        internal FeatureProcessingPipline() : this(Enumerable.Empty<T>().AsQueryable())
        {
        }

        public IQueryable<T> Data
        {
            get
            {
                return _data;
            }
        }

        public IEnumerable<IBlobStore> Outputs
        {
            get
            {
                return _outputs;
            }
        }

        public IFloatingPointFeatureExtractor<T> FeatureExtractor
        {
            get
            {
                return _transformation ?? _featureExtractor;
            }
        }

        public int VectorSize
        {
            get
            {
                return FeatureExtractor.VectorSize;
            }
        }

        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                return FeatureExtractor.FeatureMetadata;
            }
        }

        public FeatureProcessingPipline<T> FilterFeaturesByProperty(Action<PropertySelector<T>> selector)
        {
            var ps = new PropertySelector<T>();

            selector(ps);

            if (ps.SelectedProperties.Any())
            {
                FilterFeatures(f => ps.SelectedProperties.Contains(f.Label));
            }

            return this;
        }

        public FeatureProcessingPipline<T> FilterFeatures(Func<IFeature, bool> featureFilter)
        {
            if (_transformation == null)
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_featureExtractor, null, featureFilter);
            }
            else
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_featureExtractor, _transformation.Transformation, featureFilter);
            }

            return this;
        }

        public FeatureProcessingPipline<T> PreprocessWith(Func<double[], double[]> transformFunction)
        {
            if (_transformation == null)
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_featureExtractor, transformFunction);
            }
            else
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_featureExtractor, transformFunction, _transformation.FeatureFilter);
            }

            return this;
        }

        internal void OutputResults(IBinaryPersistable result, string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name is missing");

            foreach (var output in _outputs)
            {
                output.Store(name, result);
            }
        }

        internal ExecutionPipline<TResult> ProcessWith<TResult>(Func<FeatureProcessingPipline<T>, string, TResult> processor)
        {
            return new ExecutionPipline<TResult>((n) =>
            {
                var res = processor.Invoke(this, n);

                return res;
            }, (x, o) => true);
        }

        public FeatureProcessingPipline<T> OutputResultsTo(IBlobStore store)
        {
            _outputs.Add(store);

            return this;
        }

        public IEnumerable<ColumnVector1D> ExtractVectors()
        {
            if (_transformation == null)
            {
                if (_featureExtractor.IsNormalising)
                {
                    _featureExtractor.NormaliseUsing(_data);
                }

                foreach (var batch in _data.Chunk())
                {
                    foreach (var item in batch)
                    {
                        yield return _featureExtractor.ExtractColumnVector(item);
                    }
                }
            }
            else
            {
                if (_featureExtractor.IsNormalising)
                {
                    _transformation.NormaliseUsing(_data);
                }

                foreach (var batch in _data.Chunk())
                {
                    foreach (var item in batch)
                    {
                        yield return _transformation.ExtractColumnVector(item);
                    }
                }
            }
        }

        public IEnumerable<IList<ObjectVector<T>>> ExtractBatches(int batchSize = 1000)
        {
            if (_transformation == null)
            {
                if (_featureExtractor.IsNormalising)
                {
                    _featureExtractor.NormaliseUsing(_data);
                }

                foreach (var batch in _data.Chunk(batchSize))
                {
                    yield return batch.Select(b => new ObjectVector<T>(b, _featureExtractor.ExtractColumnVector(b))).ToList();
                }
            }
            else
            {
                if (_featureExtractor.IsNormalising)
                {
                    _transformation.NormaliseUsing(_data);
                }

                foreach (var batch in _data.Chunk(batchSize))
                {
                    yield return batch.Select(b => new ObjectVector<T>(b, _transformation.ExtractColumnVector(b))).ToList();
                }
            }
        }
    }
}