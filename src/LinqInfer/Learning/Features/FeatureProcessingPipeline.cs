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
        public int VectorSize => FeatureExtractor.VectorSize;

        /// <summary>
        /// Returns an enumeration of feature metadata
        /// </summary>
        public IEnumerable<IFeature> FeatureMetadata => FeatureExtractor.FeatureMetadata;

        /// <summary>
        /// Returns the number of samples in the pipeline
        /// </summary>
        public int SampleCount => Data.Count();

        /// <summary>
        /// Exports the internal state (not the data) of the pipeline as a <see cref="BinaryVectorDocument"/>
        /// </summary>
        public BinaryVectorDocument SaveState() => _featureExtractor.ToVectorDocument();

        /// <summary>
        /// Retores the state of the pipeline from a previously exported <see cref="BinaryVectorDocument"/>
        /// </summary>
        public void RestoreState(BinaryVectorDocument data) => _featureExtractor.FromVectorDocument(data);

        /// <summary>
        /// Filters features by property
        /// </summary>
        public IFeatureProcessingPipeline<T> FilterFeaturesByProperty(Action<PropertySelector<T>> selector)
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
        public IFeatureProcessingPipeline<T> FilterFeatures(Func<IFeature, bool> featureFilter)
        {
            _featureExtractor.FilterFeatures(featureFilter);

            return this;
        }

        public IFeatureProcessingPipeline<T> ScaleFeatures(Range? range = null)
        {
            if (!range.HasValue) range = new Range(1, -1);

            var minMax = ExtractColumnVectors().MinAnMaxOfEachDimension();
            var adjustedMax = minMax.Item2 - minMax.Item1;
            var scaleValue = adjustedMax / (range.Value.Max - range.Value.Min);
            var rangeMin = Vector.UniformVector(adjustedMax.Size, range.Value.Min);

            var minTranspose = new VectorOperation(VectorOperationType.Subtract, minMax.Item1);
            var scale = new VectorOperation(VectorOperationType.Divide, scaleValue);
            var rangeTranspose = new VectorOperation(VectorOperationType.Subtract, rangeMin);

            var transform = new SerialisableVectorTransformation(minTranspose, scale, rangeTranspose);

            PreprocessWith(transform);

            return this;
        }

        /// <summary>
        /// Centres the feature data by subtracting the mean from each dimension
        /// </summary>
        public IFeatureProcessingPipeline<T> CentreFeatures()
        {
            var mean = ExtractColumnVectors().MeanOfEachDimension();

            var transform = new SerialisableVectorTransformation(new VectorOperation(VectorOperationType.Subtract, mean));

            PreprocessWith(transform);

            return this;
        }

        /// <summary>
        /// Using principal component analysis, reduces the least significant features
        /// keeping a specified number of features (dimensions)
        /// </summary>
        /// <param name="numberOfDimensions">The (max) number of features to retain</param>
        /// <param name="sampleSize">The size of the sample to use for analysis</param>
        /// <returns>The feature processing pipeline with the transform applied</returns>
        public IFeatureProcessingPipeline<T> PrincipalComponentReduction(int numberOfDimensions, int sampleSize = 100)
        {
            var pca = new PrincipalComponentAnalysis(this);

            var pp = pca.CreatePrincipalComponentTransformer(numberOfDimensions, sampleSize);
            
            return PreprocessWith(pp);
        }

        /// <summary>
        /// Using a kohonen self organising map, reduces the dimensionality of the feature set
        /// </summary>
        /// <param name="numberOfDimensions">The (max) number of features to retain</param>
        /// <param name="sampleSize">The size of the sample to use for analysis</param>
        /// <param name="parameters">The parameters used to affect the clustering process</param>
        /// <returns>The feature processing pipeline with the transform applied</returns>
        public IFeatureProcessingPipeline<T> KohonenSOMFeatureReduction(int numberOfDimensions, int sampleSize = 100, ClusteringParameters parameters = null)
        {
            if (parameters == null) parameters = new ClusteringParameters()
            {
                InitialRadius = 0.5,
                InitialLearningRate = 0.1f
            };

            parameters.NumberOfOutputNodes = numberOfDimensions;

            var mapper = new FeatureMapperV3<T>(parameters);

            NormaliseData();

            var map = mapper.Map(Limit(sampleSize));

            var transform = new SerialisableVectorTransformation(new VectorOperation(VectorOperationType.EuclideanDistance, map.ExportClusterWeights()));

            return PreprocessWith(transform);
        }

        /// <summary>
        /// Preprocesses the data with the supplied transformation
        /// </summary>
        /// <param name="transformation">The vector transformation</param>
        /// <returns>The current <see cref="FeatureProcessingPipeline{T}"/></returns>
        public IFeatureProcessingPipeline<T> PreprocessWith(IVectorTransformation transformation)
        {
            _featureExtractor.PreprocessWith(transformation);

            return this;
        }

        internal FeatureProcessingPipeline<T> Limit(int numberOfSamples)
        {
            return new FeatureProcessingPipeline<T>(_data.Take(numberOfSamples), FeatureExtractor);
        }

        internal void OutputResults(IBinaryPersistable result, string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name is missing");

            foreach (var output in _outputs)
            {
                output.Store(name, result);
            }
        }

        public ExecutionPipline<TResult> ProcessWith<TResult>(Func<IFeatureProcessingPipeline<T>, string, TResult> processor)
        {
            return new ExecutionPipline<TResult>((n) =>
            {
                var res = processor.Invoke(this, n);

                return res;
            }, (x, o) => true);
        }

        public ExecutionPipline<TResult> ProcessAsyncWith<TResult>(Func<IFeatureProcessingPipeline<T>, string, Task<TResult>> processor)
        {
            return new ExecutionPipline<TResult>((n) =>
            {
                var res = processor.Invoke(this, n);

                return res;
            }, (x, o) => true);
        }

        /// <summary>
        /// Performs simple normalisation over the data,
        /// readjusting data so that values fall between 0 and 1
        /// </summary>
        public IFeatureProcessingPipeline<T> NormaliseData()
        {
            var fe = FeatureExtractor;

            CentreFeatures();
            ScaleFeatures(new Range(1, 0));

            return this;
        }

        /// <summary>
        /// Extracts vectors from the dataset
        /// </summary>
        public IEnumerable<IVector> ExtractVectors()
        {
            var fe = FeatureExtractor;

            foreach (var batch in _data.Chunk())
            {
                foreach (var item in batch)
                {
                    yield return fe.ExtractIVector(item);
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
                yield return batch.Select(b => new ObjectVector<T>(b, fe.ExtractIVector(b))).ToList();
            }
        }

        /// <summary>
        /// Extracts and exports data into a single document
        /// </summary>
        public BinaryVectorDocument ExportData(int? maxRows = null)
        {
            var doc = new BinaryVectorDocument();

            foreach(var vec in (maxRows.HasValue ? ExtractColumnVectors().Take(maxRows.Value) : ExtractColumnVectors()))
            {
                doc.Vectors.Add(vec);
            }

            return doc;
        }

        internal IEnumerable<ColumnVector1D> ExtractColumnVectors()
        {
            return ExtractVectors().Select(v => v.ToColumnVector());
        }
    }
}