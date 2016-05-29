using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    public class ExecutionPipline<TResult>
    {
        private readonly Func<string, TResult> _execute;

        internal ExecutionPipline(Func<string, TResult> execute)
        {
            _execute = execute;
        }

        public TResult Execute(string outputName = null)
        {
            return _execute(outputName);
        }
    }

    public sealed class FeatureProcessingPipline<T> where T : class
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

        internal IQueryable<T> Data
        {
            get
            {
                return _data;
            }
        }

        internal IFloatingPointFeatureExtractor<T> FeatureExtractor
        {
            get
            {
                return _transformation ?? _featureExtractor;
            }
        }

        public FeatureProcessingPipline<T> FilterFeatures(Func<IFeature, bool> featureFilter)
        {
            if (_transformation == null)
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_featureExtractor, v => v, featureFilter);
            }
            else
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_featureExtractor, _transformation.Transformation, featureFilter);
            }

            return this;
        }

        public FeatureProcessingPipline<T> PreprocessWith(Func<double[], double[]> transformFunction)
        {
            _transformation = new FloatingPointTransformingFeatureExtractor<T>(_featureExtractor, transformFunction);

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
            });
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
                foreach (var batch in _data.Chunk())
                {
                    foreach (var item in batch)
                    {
                        yield return _transformation.ExtractColumnVector(item);
                    }
                }
            }
        }
    }
}
