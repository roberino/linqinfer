using LinqInfer.Data.Serialisation;
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
        TransformingFeatureExtractor<T> _featureExtractor;

        internal FeatureProcessingPipeline(IQueryable<T> data, IFloatingPointFeatureExtractor<T> featureExtractor = null)
        {
            Data = data;
            
            _featureExtractor = featureExtractor as TransformingFeatureExtractor<T> ?? new TransformingFeatureExtractor<T>(featureExtractor ?? new ObjectFeatureExtractor<T>());
        }

        internal FeatureProcessingPipeline() : this(Enumerable.Empty<T>().AsQueryable())
        {
        }

        /// <summary>
        /// Returns the original data source
        /// </summary>
        public IQueryable<T> Data { get; }

        /// <summary>
        /// Returns the feature extractor
        /// </summary>
        public IFloatingPointFeatureExtractor<T> FeatureExtractor => _featureExtractor;

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
        /// Exports the internal state (not the data) of the pipeline as a <see cref="PortableDataDocument"/>
        /// </summary>
        public PortableDataDocument SaveState() => _featureExtractor.ExportData();

        /// <summary>
        /// Retores the state of the pipeline from a previously exported <see cref="PortableDataDocument"/>
        /// </summary>
        public void RestoreState(PortableDataDocument data) => _featureExtractor = TransformingFeatureExtractor<T>.Create(data);
        
        public IFeatureProcessingPipeline<T> ScaleFeatures(Range? range = null)
        {
            var minMax = ExtractColumnVectors().MinMaxAndMeanOfEachDimension();
            var transform = minMax.CreateScaleTransformation(range);
            return PreprocessWith(transform);
        }

        /// <summary>
        /// Centres the feature data by subtracting the mean from each dimension
        /// </summary>
        public IFeatureProcessingPipeline<T> CentreFeatures()
        {
            var mean = ExtractColumnVectors().MeanOfEachDimension();

            var transform = new SerialisableDataTransformation(new DataOperation(VectorOperationType.Subtract, mean));

            return PreprocessWith(transform);
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
        /// <param name="configure">Configure the parameters used to affect the clustering process</param>
        /// <returns>The feature processing pipeline with the transform applied</returns>
        public IFeatureProcessingPipeline<T> KohonenSomFeatureReduction(int numberOfDimensions, int sampleSize = 100, Action<ClusteringParameters> configure = null)
        {
            var parameters = new ClusteringParameters()
            {
                InitialRadius = 0.5,
                InitialLearningRate = 0.1f,
                NumberOfOutputNodes = numberOfDimensions
            };

            parameters.NumberOfOutputNodes = numberOfDimensions;

            configure?.Invoke(parameters);

            var mapper = new FeatureMapperV3<T>(parameters);

            NormaliseData();

            var map = mapper.Map(Limit(sampleSize));

            var transform = new SerialisableDataTransformation(new DataOperation(VectorOperationType.EuclideanDistance, map.ExportClusterWeights()));

            return PreprocessWith(transform);
        }

        /// <summary>
        /// Preprocesses the data with the supplied transformation
        /// </summary>
        /// <param name="transformation">The vector transformation</param>
        /// <returns>The current <see cref="FeatureProcessingPipeline{T}"/></returns>
        public IFeatureProcessingPipeline<T> PreprocessWith(ISerialisableDataTransformation transformation)
        {
            _featureExtractor.AddTransform(transformation);

            return this;
        }

        internal FeatureProcessingPipeline<T> Limit(int numberOfSamples)
        {
            return new FeatureProcessingPipeline<T>(Data.Take(numberOfSamples), FeatureExtractor);
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
            try
            {
                return CentreFeatures().ScaleFeatures(Range.ZeroToOne);
            }
            catch (Exception ex)
            {
                DebugOutput.Log(ex);

                DebugOutput.Log(ExtractColumnVectors());

                throw;
            }
        }

        /// <summary>
        /// Extracts vectors from the dataset
        /// </summary>
        public IEnumerable<IVector> ExtractVectors()
        {
            var fe = FeatureExtractor;

            foreach (var batch in Data.Chunk())
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
        public IEnumerable<IList<ObjectVectorPair<T>>> ExtractBatches(int batchSize = 1000)
        {
            var fe = FeatureExtractor;

            foreach (var batch in Data.Chunk(batchSize))
            {
                yield return batch.Select(b => new ObjectVectorPair<T>(b, fe.ExtractIVector(b))).ToList();
            }
        }

        /// <summary>
        /// Extracts and exports data into a single document
        /// </summary>
        public PortableDataDocument ExportData(int? maxRows = null)
        {
            var doc = new PortableDataDocument();

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