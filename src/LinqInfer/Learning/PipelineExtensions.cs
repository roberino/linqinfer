using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

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
        public static IFeatureTransformBuilder<T> CreateFeatureTransformation<T>(IVectorFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipeline<T>(Enumerable.Empty<T>().AsQueryable(), featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="data">The data source</param>
        /// <param name="vectorExpression">A function which extracts a feature vector from an object instance. 
        /// This function is called to established the vector size so even default or null value passed to the function should return an array</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<T> CreatePipeline<T>(this IQueryable<T> data, Expression<Func<T, IVector>> vectorExpression, string[] featureLabels = null) where T : class
        {
            var vectorFunc = vectorExpression.Compile();
            var size = featureLabels?.Length ?? vectorFunc(DefaultOf<T>()).Size;
            var featureExtractor = new ExpressionFeatureExtractor<T>(vectorExpression, size, Feature.CreateDefaults(featureLabels ?? Enumerable.Range(1, size).Select(n => n.ToString())));
            var pipeline = new FeatureProcessingPipeline<T>(data, featureExtractor);

            return pipeline;
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="vectorExpression">A function which extracts a feature vector from an object instance. 
        /// This function is called to established the vector size so even default or null value passed to the function should return an array</param>
        /// <param name="vectorSize">The size of the vector returned by the vector function. 
        /// IF YOU DO NOT provide this value, the vector function will be called with default arguments which may be null</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<T> CreatePipeline<T>(this IQueryable<T> data, Expression<Func<T, IVector>> vectorExpression, int vectorSize) where T : class
        {
            Contract.Assert(vectorSize > 0);

            var featureExtractor = new ExpressionFeatureExtractor<T>(vectorExpression, vectorSize);

            return new FeatureProcessingPipeline<T>(data, featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="data">The data</param>
        /// <param name="featureExtractor">An optional feature extractor to extract feature vectors from the data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<T> CreatePipeline<T>(this IQueryable<T> data, IVectorFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipeline<T>(data, featureExtractor ?? new ObjectFeatureExtractor<T>());
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of vectors.
        /// </summary>
        /// <param name="data">The data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<ColumnVector1D> CreatePipeline(this IQueryable<ColumnVector1D> data)
        {
            var first = data.FirstOrDefault();
            return CreatePipeline(data, v => v, first?.Size ?? 0);
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
            return CreatePipeline(featureMap.AsQueryable(), v => v.Weights, first == null ? 0 : first.Weights.Size);
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
        public static ITrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(this IQueryable<TrainingPair<TInput, TClass>> trainingData, IVectorFeatureExtractor<TInput> featureExtractor)
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

        static T DefaultOf<T>() where T : class
        {
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch
            {
                return default;
            }
        }
    }
}