using LinqInfer.Data;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
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
            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(vectorFunc, size, normaliseData, featureLabels ?? Enumerable.Range(1, size).Select(n => n.ToString()).ToArray());
            return new FeatureProcessingPipeline<T>(data, featureExtractor);
        }

        /// <summary>
        /// Creates a feature pipeline from a single document of vector data
        /// </summary>
        /// <param name="data">The document data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipeline<Vector> CreatePipeline(this BinaryVectorDocument data)
        {
            if (!data.Vectors.Any())
            {
                throw new ArgumentException("No data found");
            }

            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<Vector>(v => v.GetUnderlyingArray(), data.Vectors.First().Size, false);

            return new FeatureProcessingPipeline<Vector>(data.Vectors.AsQueryable(), featureExtractor);
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

            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(vectorFunc, vectorSize, true, Enumerable.Range(1, vectorSize).Select(n => n.ToString()).ToArray());
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
        public static ITrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(this FeatureProcessingPipeline<TInput> pipeline, Expression<Func<TInput, TClass>> classf)
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
        public static ExecutionPipline<Matrix> ToMatrix<TInput>(this FeatureProcessingPipeline<TInput> pipeline, int limit = 100) where TInput : class
        {
            Contract.Assert(limit > 0);

            return pipeline.ProcessWith((p, n) =>
            {
                return new Matrix(p.ExtractVectors().Take(limit));
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
        public static ExecutionPipline<TextWriter> ToCsv<TInput>(this FeatureProcessingPipeline<TInput> pipeline, TextWriter writer, Func<TInput, string> label = null, char delimitter = ',') where TInput : class
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
                        writer.WriteLine(m.Vector.ToCsv(delimitter));
                    }
                }

                return writer;
            });
        }

        /// <summary>
        /// Creates a self-organising feature map using the supplied feature data. Items will be clustered based on Euclidean distance.
        /// If an initial node radius is supplied, a Kohonen SOM implementation will be used, otherwise a simpler
        /// k-means centroid calculation will be used.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="outputNodeCount">The maximum number of output nodes</param>
        /// <param name="learningRate">The learning rate</param>
        /// <param name="initialNodeRadius">When supplied, this is used used to determine the radius of each cluster node 
        /// which is used to calculate the influence a node has on neighbouring nodes when updating weights</param>
        /// <returns>An execution pipeline for creating a SOFM</returns>
        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this FeatureProcessingPipeline<TInput> pipeline, int outputNodeCount = 10, float learningRate = 0.5f, float? initialNodeRadius = null, int trainingEpochs = 1000) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapperV3<TInput>(outputNodeCount, learningRate, trainingEpochs, initialNodeRadius);

                pipeline.NormaliseData();

                return fm.Map(p);
            });
        }

        /// <summary>
        /// Creates a self-organising feature map using the supplied feature data. Items will be clustered based on Euclidean distance.
        /// If an initial node radius is supplied, a Kohonen SOM implementation will be used, otherwise a simpler
        /// k-means centroid calculation will be used.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="parameters">The parameters</param>
        /// <returns></returns>
        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this FeatureProcessingPipeline<TInput> pipeline, ClusteringParameters parameters)
             where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapperV3<TInput>(parameters);

                pipeline.NormaliseData();

                return fm.Map(p);
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
        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToNaiveBayesClassifier<TInput, TClass>(this FeatureProcessingPipeline<TInput> pipeline, Expression<Func<TInput, TClass>> classf) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var net = new NaiveBayesNormalClassifier<TClass>(p.FeatureExtractor.VectorSize);
                var classifierPipe = new ClassificationPipeline<TClass, TInput, double>(net, net, p.FeatureExtractor);

                p.NormaliseData();

                classifierPipe.Train(p.Data, classf);

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

                classifierPipe.Train(pipeline.Data, trainingSet.ClassifyingExpression);

                return (IObjectClassifier<TClass, TInput>)classifierPipe;
            });
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied training data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="trainingSet">A training set</param>
        /// <param name="learningRate">The learning rate (rate of neuron weight adjustment when training)</param>
        /// <param name="hiddenLayers">The number of neurons in each respective hidden layer</param>
        /// <returns>An executable object which produces a classifier</returns>
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToSoftmaxClassifier<TInput, TClass>(
            this ITrainingSet<TInput, TClass> trainingSet,
            double learningRate = 0.1d) where TInput : class where TClass : IEquatable<TClass>
        {
            var trainingPipline = new MultilayerNetworkTrainingRunner<TClass, TInput>(trainingSet);
            var pipeline = trainingSet.FeaturePipeline;
            var inputSize = pipeline.VectorSize;
            var outputSize = trainingPipline.OutputMapper.VectorSize;

            var parameters = NetworkParameters.Softmax(new[] { inputSize, inputSize * outputSize, outputSize });

            parameters.LearningRate = learningRate;

            parameters.Validate();

            var strategy = new StaticParametersMultilayerTrainingStrategy<TClass, TInput>(parameters);

            return pipeline.ProcessWith((p, n) =>
            {
                var result = trainingPipline.TrainUsing(strategy).Result;

                if (n != null) pipeline.OutputResults(result, n);

                return result;
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