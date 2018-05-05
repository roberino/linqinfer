using LinqInfer.Data;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning
{
    public static class PipelineExtensions
    {
        /// <summary>
        /// Returns an object for transforming and working with features.
        /// </summary>
        /// <typeparam name="T">The input type</typeparam>
        /// <param name="featureExtractor">An optional feature extractor</param>
        /// <returns></returns>
        public static IFeatureTransformBuilder<T> CreateFeatureTransformation<T>(IFloatingPointFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipeline<T>(Enumerable.Empty<T>().AsQueryable(), featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="vectorFunc">A function which extracts a feature vector from an object instance. 
        /// This function is called to established the vector size so even default or null value passed to the function should return an array</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<T> CreatePipeline<T>(this IQueryable<T> data, Func<T, double[]> vectorFunc, bool normaliseData = true, string[] featureLabels = null) where T : class
        {
            var size = featureLabels == null ? vectorFunc(DefaultOf<T>()).Length : featureLabels.Length;
            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(vectorFunc, size, featureLabels ?? Enumerable.Range(1, size).Select(n => n.ToString()).ToArray());
            var pipeline = new FeatureProcessingPipeline<T>(data, featureExtractor);

            if (normaliseData)
            {
                pipeline.NormaliseData();
            }

            return pipeline;
        }

        /// <summary>
        /// Creates a feature pipeline from a single document of vector data
        /// </summary>
        /// <param name="data">The document data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<Vector> CreatePipeline(this PortableDataDocument data)
        {
            if (!data.Vectors.Any())
            {
                throw new ArgumentException("No data found");
            }

            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<Vector>(v => v.GetUnderlyingArray(), data.Vectors.First().Size);

            return new FeatureProcessingPipeline<Vector>(data.Vectors.Select(v => v.ToColumnVector()).AsQueryable(), featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="vectorFunc">A function which extracts a feature vector from an object instance. 
        /// This function is called to established the vector size so even default or null value passed to the function should return an array</param>
        /// <param name="vectorSize">The size of the vector returned by the vector function. 
        /// IF YOU DO NOT provide this value, the vector function will be called with default arguments which may be null</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<T> CreatePipeline<T>(this IQueryable<T> data, Func<T, double[]> vectorFunc, int vectorSize) where T : class
        {
            Contract.Assert(vectorSize > 0);

            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(vectorFunc, vectorSize, Enumerable.Range(1, vectorSize).Select(n => n.ToString()).ToArray());
            return new FeatureProcessingPipeline<T>(data, featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="data">The data</param>
        /// <param name="featureExtractor">An optional feature extractor to extract feature vectors from the data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<T> CreatePipeline<T>(this IQueryable<T> data, IFloatingPointFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipeline<T>(data, featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of vectors.
        /// </summary>
        /// <param name="data">The data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<ColumnVector1D> CreatePipeline(this IQueryable<ColumnVector1D> data)
        {
            var first = data.FirstOrDefault();
            return CreatePipeline(data, v => v.GetUnderlyingArray(), first == null ? 0 : first.Size);
        }

        /// <summary>
        /// Creates a pipeline of feature mapped data.
        /// </summary>
        /// <typeparam name="T">The input type</typeparam>
        /// <param name="featureMap">The feature map</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<ClusterNode<T>> CreatePipeine<T>(this FeatureMap<T> featureMap) where T : class
        {
            var first = featureMap.FirstOrDefault();
            return CreatePipeline(featureMap.AsQueryable(), v => v.Weights.GetUnderlyingArray(), first == null ? 0 : first.Weights.Size);
        }

        /// <summary>
        /// Creates a training set from a feature pipeline
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="classf">A classifying expression</param>
        /// <returns>A training set</returns>
        public static ITrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(this IFeatureProcessingPipeline<TInput> pipeline, Expression<Func<TInput, TClass>> classf)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            return new TrainingSet<TInput, TClass>(pipeline, classf);
        }

        /// <summary>
        /// Creates a training set from a feature pipeline
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="classf">A classifying expression</param>
        /// <returns>A training set</returns>
        public static ITrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(this IQueryable<TrainingPair<TInput, TClass>> trainingData, IFloatingPointFeatureExtractor<TInput> featureExtractor)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var pipeline = new FeatureProcessingPipeline<TInput>(trainingData.Select(d => d.Input), featureExtractor);

            throw new NotImplementedException();

            //return new TrainingSet<TInput, TClass>(pipeline, null);
        }

        /// <summary>
        /// Extracts vector data as a matrix
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="limit">The maximum amount of data</param>
        /// <returns>A <see cref="ExecutionPipline{Matrix}"/></returns>
        public static ExecutionPipline<Matrix> ToMatrix<TInput>(this IFeatureProcessingPipeline<TInput> pipeline, int limit = 100) where TInput : class
        {
            Contract.Assert(limit > 0);

            return pipeline.ProcessWith((p, n) =>
            {
                return new Matrix(p.ExtractVectors().Select(v => v.ToColumnVector()).Take(limit));
            });
        }

        /// <summary>
        /// Writes the extracted data as CSV
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="writer">A text writer</param>
        /// <param name="label">An optional function which will label each row (as the first column)</param>
        /// <returns></returns>
        public static ExecutionPipline<TextWriter> ToCsv<TInput>(this IFeatureProcessingPipeline<TInput> pipeline, TextWriter writer, Func<TInput, string> label = null, char delimitter = ',') where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                foreach (var batch in p.ExtractBatches())
                {
                    foreach (var m in batch)
                    {
                        if (label != null)
                        {
                            writer.Write("\"" + label(m.Value).Replace("\"", "\\\"") + "\"" + delimitter);
                        }
                        writer.WriteLine(m.ColumnVector.ToCsv(delimitter));
                    }
                }

                return writer;
            });
        }

        /// <summary>
        /// Creates a basic Naive Bayesian classifiers, training the classifier using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <returns></returns>
        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToNaiveBayesClassifier<TInput, TClass>(this IFeatureProcessingPipeline<TInput> pipeline, Expression<Func<TInput, TClass>> classf) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var net = new NaiveBayesNormalClassifier<TClass>(p.FeatureExtractor.VectorSize);
                var classifierPipe = new ClassificationPipeline<TClass, TInput, double>(net, net, p.FeatureExtractor);

                p.NormaliseData();

                classifierPipe.Train(((FeatureProcessingPipeline<TInput>)p).Data, classf);

                return (IObjectClassifier<TClass, TInput>)classifierPipe;
            });
        }

        /// <summary>
        /// Creates a basic Naive Bayesian classifiers, training the classifier using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <returns></returns>
        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToNaiveBayesClassifier<TInput, TClass>(this ITrainingSet<TInput, TClass> trainingSet) where TInput : class where TClass : IEquatable<TClass>
        {
            var pipeline = trainingSet.FeaturePipeline;

            return pipeline.ProcessWith((p, n) =>
            {
                var net = new NaiveBayesNormalClassifier<TClass>(p.FeatureExtractor.VectorSize);
                var classifierPipe = new ClassificationPipeline<TClass, TInput, double>(net, net, p.FeatureExtractor);

                p.NormaliseData();

                classifierPipe.Train(((FeatureProcessingPipeline<TInput>)p).Data, trainingSet.ClassifyingExpression);

                return (IObjectClassifier<TClass, TInput>)classifierPipe;
            });
        }

        private static T DefaultOf<T>() where T : class
        {
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch
            {
                return default(T);
            }
        }
    }
}