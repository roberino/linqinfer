using LinqInfer.Maths;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Utility;
using System;
using LinqInfer.Data;

namespace LinqInfer.Learning.Features
{
    public class ExecutionPipline<TResult>
    {
        private readonly Func<string, TResult> _execute;

        public ExecutionPipline(Func<string, TResult> execute)
        {
            _execute = execute;
        }

        public TResult Execute(string outputName = null)
        {
            return _execute(outputName);
        }
    }

    public sealed class FeaturePipline<T> where T : class
    {
        private static readonly ObjectFeatureExtractor _objExtractor = new ObjectFeatureExtractor();

        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly IQueryable<T> _data;
        private readonly IList<IBlobStore> _outputs;

        internal FeaturePipline(IQueryable<T> data, IFloatingPointFeatureExtractor<T> featureExtractor = null)
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
                return _featureExtractor;
            }
        }

        internal IFloatingPointFeatureExtractor<ColumnVector1D> Preprocessor { get; set; }

        //public FeaturePipline<T> PreprocessWith(IFloatingPointFeatureExtractor<ColumnVector1D> preprocessor)
        //{
        //    Preprocessor = preprocessor;

        //    return this;
        //}

        internal void OutputResults(IBinaryPersistable result, string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name is missing");

            foreach (var output in _outputs)
            {
                output.Store(name, result);
            }
        }

        internal ExecutionPipline<TResult> ProcessWith<TResult>(Func<FeaturePipline<T>, string, TResult> processor)
        {
            return new ExecutionPipline<TResult>((n) =>
            {
                var res = processor.Invoke(this, n);

                return res;
            });
        }

        public FeaturePipline<T> OutputResultsTo(IBlobStore store)
        {
            _outputs.Add(store);

            return this;
        }

        public IEnumerable<ColumnVector1D> ExtractVectors()
        {
            var preprocess = Preprocessor;

            if (preprocess == null)
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
                        yield return preprocess.ExtractColumnVector(_featureExtractor.ExtractColumnVector(item));
                    }
                }
            }
        }
    }
}
