using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    public sealed class FeatureProcessingPipeline<T> : IFeatureProcessingPipeline<T> where T : class
    {
        private readonly MultiFunctionFeatureExtractor<T> _featureExtractor;
        private readonly IQueryable<T> _data;
        private readonly IList<IBlobStore> _outputs;

        internal FeatureProcessingPipeline(IQueryable<T> data, IFloatingPointFeatureExtractor<T> featureExtractor = null)
        {
            _data = data;
            _featureExtractor = new MultiFunctionFeatureExtractor<T>(featureExtractor);
            _outputs = new List<IBlobStore>();
        }

        internal FeatureProcessingPipeline() : this(Enumerable.Empty<T>().AsQueryable())
        {
        }

        /// <summary>
        /// Returns the original data source
        /// </summary>
        public IQueryable<T> Data
        {
            get
            {
                return _data;
            }
        }

        /// <summary>
        /// Returns the outputs
        /// </summary>
        public IEnumerable<IBlobStore> Outputs
        {
            get
            {
                return _outputs;
            }
        }

        /// <summary>
        /// Returns the feature extractor
        /// </summary>
        public IFloatingPointFeatureExtractor<T> FeatureExtractor
        {
            get
            {
                return _featureExtractor;
            }
        }

        /// <summary>
        /// Returns the size of the vector returned
        /// when vectors are extracted
        /// </summary>
        public int VectorSize
        {
            get
            {
                return FeatureExtractor.VectorSize;
            }
        }

        /// <summary>
        /// Returns an enumeration of feature metadata
        /// </summary>
        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                return FeatureExtractor.FeatureMetadata;
            }
        }

        /// <summary>
        /// Exports the internal state (not the data) of the pipeline as a <see cref="BinaryVectorDocument"/>
        /// </summary>
        public BinaryVectorDocument SaveState()
        {
            return _featureExtractor.ToVectorDocument();
        }

        /// <summary>
        /// Retores the state of the pipeline from a previously exported <see cref="BinaryVectorDocument"/>
        /// </summary>
        public void RestoreState(BinaryVectorDocument data)
        {
            _featureExtractor.FromVectorDocument(data);
        }

        /// <summary>
        /// Filters features by property
        /// </summary>
        public FeatureProcessingPipeline<T> FilterFeaturesByProperty(Action<PropertySelector<T>> selector)
        {
            var ps = new PropertySelector<T>();

            selector(ps);

            if (ps.SelectedProperties.Any())
            {
                FilterFeatures(f => ps.SelectedProperties.Contains(f.Label));
            }

            return this;
        }

        /// <summary>
        /// Filters features using the specified predicate function
        /// </summary>
        public FeatureProcessingPipeline<T> FilterFeatures(Func<IFeature, bool> featureFilter)
        {
            _featureExtractor.FilterFeatures(featureFilter);

            return this;
        }

        /// <summary>
        /// Using principal component analysis, reduces the least significant features
        /// keeping a specified number of features (dimensions)
        /// </summary>
        /// <param name="numberOfDimensions">The (max) number of features to retain</param>
        /// <param name="sampleSize">The size of the sample to use for analysis</param>
        /// <returns>The feature processing pipeline with the transform applied</returns>
        public FeatureProcessingPipeline<T> PrincipalComponentReduction(int numberOfDimensions, int sampleSize = 100)
        {
            var pca = new PrincipalComponentAnalysis(this);

            return PreprocessWith(pca.CreatePrincipalComponentTransformer(numberOfDimensions, sampleSize));
        }

        /// <summary>
        /// Preprocesses the data with the supplied transformation
        /// </summary>
        /// <param name="transformation">The vector transformation</param>
        /// <returns>The current <see cref="FeatureProcessingPipeline{T}"/></returns>
        public FeatureProcessingPipeline<T> PreprocessWith(IVectorTransformation transformation)
        {
            _featureExtractor.PreprocessWith(transformation);

            return this;
        }

        /// <summary>
        /// Preprocesses the data with transforming function
        /// (only one supported currently)
        /// The transforming function may or may not
        /// change the size of the extracted vector.
        /// Function transformations are not serialisable.
        /// </summary>
        /// <param name="transformFunction">The transforming function</param>
        /// <returns>The current <see cref="FeatureProcessingPipeline{T}"/></returns>
        public FeatureProcessingPipeline<T> PreprocessWith(Func<double[], double[]> transformFunction)
        {
            PreprocessWith(new DelegateVectorTransformation(_featureExtractor.VectorSize, transformFunction));

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

        internal ExecutionPipline<TResult> ProcessWith<TResult>(Func<FeatureProcessingPipeline<T>, string, TResult> processor)
        {
            return new ExecutionPipline<TResult>((n) =>
            {
                var res = processor.Invoke(this, n);

                return res;
            }, (x, o) => true);
        }

        internal ExecutionPipline<TResult> ProcessAsyncWith<TResult>(Func<FeatureProcessingPipeline<T>, string, Task<TResult>> processor)
        {
            return new ExecutionPipline<TResult>((n) =>
            {
                var res = processor.Invoke(this, n);

                return res;
            }, (x, o) => true);
        }

        /// <summary>
        /// Specifies where output should be stored
        /// </summary>
        /// <param name="store"></param>
        [Obsolete]
        public FeatureProcessingPipeline<T> OutputResultsTo(IBlobStore store)
        {
            _outputs.Add(store);

            return this;
        }

        /// <summary>
        /// Performs simple normalisation over the data,
        /// readjusting data so that values fall between 0 and 1
        /// </summary>
        public IFeatureProcessingPipeline<T> NormaliseData()
        {
            var fe = FeatureExtractor;

            if (fe.IsNormalising && !_featureExtractor.NormalisationCompleted)
            {
                fe.NormaliseUsing(_data);
            }

            return this;
        }

        /// <summary>
        /// Extracts vectors from the dataset
        /// </summary>
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

        /// <summary>
        /// Extracts object vector pairs in batches
        /// </summary>
        public IEnumerable<IList<ObjectVector<T>>> ExtractBatches(int batchSize = 1000)
        {
            var fe = FeatureExtractor;

            NormaliseData();

            foreach (var batch in _data.Chunk(batchSize))
            {
                yield return batch.Select(b => new ObjectVector<T>(b, fe.ExtractColumnVector(b))).ToList();
            }
        }

        /// <summary>
        /// Extracts and exports data into a single document
        /// </summary>
        public BinaryVectorDocument ExportData(int? maxRows = null)
        {
            var doc = new BinaryVectorDocument();

            foreach(var vec in (maxRows.HasValue ? ExtractVectors().Take(maxRows.Value) : ExtractVectors()))
            {
                doc.Vectors.Add(vec);
            }

            return doc;
        }
    }
}