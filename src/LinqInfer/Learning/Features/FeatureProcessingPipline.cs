using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    public sealed class ExecutionPipline<TResult>
    {
        private const int _errorThreshold = 15;
        private readonly Func<string, Task<TResult>> _execute;
        private readonly Func<bool, TResult, bool> _feedback;
        private readonly IList<Exception> _errors;
        private readonly Stopwatch _timer;

        internal ExecutionPipline(Func<string, TResult> execute, Func<bool, TResult, bool> feedback)
        {
            _execute = s => Task.FromResult(execute(s));
            _feedback = feedback;
            _errors = new List<Exception>();
            _timer = new Stopwatch();
        }

        internal ExecutionPipline(Func<string, Task<TResult>> execute, Func<bool, TResult, bool> feedback)
        {
            _execute = execute;
            _feedback = feedback;
            _errors = new List<Exception>();
            _timer = new Stopwatch();
        }

        public TimeSpan Elapsed { get { return _timer.Elapsed; } }

        public async Task<TResult> ExecuteAsync(string outputName = null)
        {
            try
            {
                _timer.Start();

                var res = await _execute(outputName);

                _feedback(true, res);

                _timer.Stop();

                return res;
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _errors.Add(ex);
                throw;
            }
        }

        public TResult Execute(string outputName = null)
        {
            try
            {
                _timer.Start();

                var task = _execute(outputName);

                task.Wait();

                var res = task.Result;

                _feedback(true, res);

                _timer.Stop();

                return res;
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _errors.Add(ex);
                throw;
            }
        }

        public TResult ExecuteUntil(Func<TResult, bool> condition, string outputName = null)
        {
            int errorStart = _errors.Count;
            
            while (true)
            {
                if (!_timer.IsRunning) _timer.Start();

                var nextTask = _execute(outputName);

                nextTask.Wait();

                var next = nextTask.Result;

                try
                {
                    if (condition(next))
                    {
                        if (_feedback(true, next))
                        {
                            _timer.Stop();

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
                    _timer.Stop();

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
        private static readonly ObjectFeatureExtractorFactory _objExtractor = new ObjectFeatureExtractorFactory();

        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly IQueryable<T> _data;
        private readonly IList<IBlobStore> _outputs;

        private FloatingPointTransformingFeatureExtractor<T> _transformation;

        private bool _normalisationCompleted;

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

                _normalisationCompleted = false;
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

            _normalisationCompleted = false;

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

            _normalisationCompleted = false;

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

        internal ExecutionPipline<TResult> ProcessAsyncWith<TResult>(Func<FeatureProcessingPipline<T>, string, Task<TResult>> processor)
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

        public IFeatureProcessingPipeline<T> NormaliseData()
        {
            var fe = FeatureExtractor;

            if (fe.IsNormalising && !_normalisationCompleted)
            {
                fe.NormaliseUsing(_data);

                _normalisationCompleted = true;
            }

            return this;
        }

        public IEnumerable<ColumnVector1D> ExtractVectors()
        {
            var fe = FeatureExtractor;

            NormaliseData();

            foreach (var batch in _data.Chunk())
            {
                foreach (var item in batch)
                {
                    yield return fe.ExtractColumnVector(item);
                }
            }
        }

        public IEnumerable<IList<ObjectVector<T>>> ExtractBatches(int batchSize = 1000)
        {
            var fe = FeatureExtractor;

            NormaliseData();

            foreach (var batch in _data.Chunk(batchSize))
            {
                yield return batch.Select(b => new ObjectVector<T>(b, fe.ExtractColumnVector(b))).ToList();
            }
        }
    }
}