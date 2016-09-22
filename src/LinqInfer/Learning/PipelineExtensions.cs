using LinqInfer.Data;
using LinqInfer.Learning.Classification;
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
        public static IFeatureTransformBuilder<T> CreateFeatureTransformation<T>(IFloatingPointFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipline<T>(Enumerable.Empty<T>().AsQueryable(), featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="vectorFunc">A function which extracts a feature vector from an object instance. 
        /// This function is called to established the vector size so even default or null value passed to the function should return an array</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipline<T> CreatePipeline<T>(this IQueryable<T> data, Func<T, double[]> vectorFunc, bool normaliseData = true, string[] featureLabels = null) where T : class
        {
            var size = featureLabels == null ? vectorFunc(DefaultOf<T>()).Length : featureLabels.Length;
            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(vectorFunc, size, normaliseData, featureLabels ?? Enumerable.Range(1, size).Select(n => n.ToString()).ToArray());
            return new FeatureProcessingPipline<T>(data, featureExtractor);
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
        public static FeatureProcessingPipline<T> CreatePipeline<T>(this IQueryable<T> data, Func<T, double[]> vectorFunc, int vectorSize) where T : class
        {
            Contract.Assert(vectorSize > 0);

            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(vectorFunc, vectorSize, true, Enumerable.Range(1, vectorSize).Select(n => n.ToString()).ToArray());
            return new FeatureProcessingPipline<T>(data, featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="data">The data</param>
        /// <param name="featureExtractor">An optional feature extractor to extract feature vectors from the data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipline<T> CreatePipeline<T>(this IQueryable<T> data, IFloatingPointFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipline<T>(data, featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of vectors.
        /// </summary>
        /// <param name="data">The data</param>
        /// <returns>A feature processing pipeline</returns>
        public static FeatureProcessingPipline<ColumnVector1D> CreatePipeline(this IQueryable<ColumnVector1D> data)
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
        public static FeatureProcessingPipline<ClusterNode<T>> CreatePipeine<T>(this FeatureMap<T> featureMap) where T : class
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
        public static ITrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(this FeatureProcessingPipline<TInput> pipeline, Expression<Func<TInput, TClass>> classf)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            return new TrainingSet<TInput, TClass>(pipeline, classf);
        }

        /// <summary>
        /// Creates a self-organising feature map using the supplied feature data. Items will be clustered based on Euclidean distance.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="normalisingSample">A sample which will be used to normalise the data (can be null if the feature extractor supports it)</param>
        /// <param name="outputNodeCount">The maximum number of output nodes</param>
        /// <param name="normaliseData">True if the data should be normalised prior to mapping</param>
        /// <param name="learningRate">The learning rate</param>
        /// <returns></returns>
        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this FeatureProcessingPipline<TInput> pipeline, TInput normalisingSample, int outputNodeCount = 10, bool normaliseData = true, float learningRate = 0.5f) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapper<TInput>(p.FeatureExtractor, normalisingSample, outputNodeCount, learningRate);

                return fm.Map(p.Data);
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
        public static ExecutionPipline<TextWriter> ToCsv<TInput>(this FeatureProcessingPipline<TInput> pipeline, TextWriter writer, Func<TInput, string> label = null) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                foreach (var batch in p.ExtractBatches())
                {
                    foreach (var m in batch)
                    {
                        if (label != null)
                        {
                            writer.Write("\"" + label(m.Value).Replace("\"", "\\\"") + "\",");
                        }
                        writer.WriteLine(m.Vector.ToCsv());
                    }
                }

                return writer;
            });
        }

        /// <summary>
        /// Creates a self-organising feature map using the supplied feature data. Items will be clustered based on Euclidean distance.
        /// If an initial node radius is supplied, a Kohonen SOFM implementation will be used, otherwise a simpler
        /// k-means centroid calculation will be used.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="outputNodeCount">The maximum number of output nodes</param>
        /// <param name="learningRate">The learning rate</param>
        /// <param name="initialNodeRadius">When supplied, this is used used to determine the radius of each cluster node 
        /// which is used to calculate the influence a node has on neighbouring nodes when updating weights</param>
        /// <param name="initialiser">An initialisation function used to determine the initial value of a output nodes weights, given the output node index</param>
        /// <returns>An execution pipeline for creating a SOFM</returns>
        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this FeatureProcessingPipline<TInput> pipeline, int outputNodeCount = 10, float learningRate = 0.5f, float? initialNodeRadius = null, Func<int, ColumnVector1D> initialiser = null) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapperV2<TInput>(outputNodeCount, learningRate, initialNodeRadius, initialiser);

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
        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToNaiveBayesClassifier<TInput, TClass>(this FeatureProcessingPipline<TInput> pipeline, Expression<Func<TInput, TClass>> classf) where TInput : class
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
        /// Creates a multi-layer neural network classifier, training the network using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <param name="errorTolerance">The network error tolerance</param>
        /// <param name="fitnessFunction">An optional fitness function which is used to determine the best solution found during the training phase</param>
        /// <param name="haltingFunction">An optional halting function which halts training once a solution is found (or not) 
        /// - the function takes the current fittest network, an interation index, and the elapsed time as parameters and
        /// return a true to indicate the training should stop</param>
        /// <returns></returns>
        public static ExecutionPipline<IPrunableObjectClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this FeatureProcessingPipline<TInput> pipeline, 
            Expression<Func<TInput, TClass>> classf, 
            float errorTolerance = 0.1f,
            Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<TClass, NetworkParameters>, double> fitnessFunction = null,
            Func<IClassifierTrainingContext<TClass, NetworkParameters>, int, TimeSpan, bool> haltingFunction = null) where TInput : class where TClass : IEquatable<TClass>
        {
            var defaultStrategy = new MaximumFitnessMultilayerNetworkTrainingStrategy<TClass, TInput>(errorTolerance, fitnessFunction, haltingFunction);

            return ToMultilayerNetworkClassifier(pipeline, classf, defaultStrategy);
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <param name="errorTolerance">The network error tolerance</param>
        /// <returns></returns>
        public static ExecutionPipline<IPrunableObjectClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this FeatureProcessingPipline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            params int[] hiddenLayers) where TInput : class where TClass : IEquatable<TClass>
        {
            var trainingSet = new TrainingSet<TInput, TClass>(pipeline, classf);
            var trainingPipline = new MultilayerNetworkTrainingRunner<TClass, TInput>(trainingSet);

            var inputSize = pipeline.VectorSize;
            var outputSize = trainingPipline.OutputMapper.VectorSize;

            var parameters = NetworkParameters.Sigmoidal(new[] { inputSize }.Concat(hiddenLayers).Concat(new[] { outputSize }).ToArray());

            parameters.Validate();

            var strategy = new StaticParametersMultilayerTrainingStrategy<TClass, TInput>(parameters);

            return pipeline.ProcessWith((p, n) =>
            {
                var result = trainingPipline.TrainUsing(strategy).Result;

                if (n != null) pipeline.OutputResults(result, n);

                return result;
            });
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied feature data and training strategy.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <param name="trainingStrategy">A implementation of a multilayer network training strategy</param>
        /// <returns></returns>
        public static ExecutionPipline<IPrunableObjectClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this FeatureProcessingPipline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            IAsyncMultilayerNetworkTrainingStrategy<TClass, TInput> trainingStrategy) where TInput : class where TClass : IEquatable<TClass>
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var trainingSet = new TrainingSet<TInput, TClass>(pipeline, classf);
                var trainingPipline = new MultilayerNetworkTrainingRunner<TClass, TInput>(trainingSet);
                var result = trainingPipline.TrainUsing(trainingStrategy).Result;

                if (n != null) pipeline.OutputResults(result, n);

                return result;
            });
        }

        /// <summary>
        /// Restores a previously saved multi-layer network classifier from a blob store.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="store">A blob store</returns>
        /// <param name="key">The name of a previously stored classifier</returns>
        public static IPrunableObjectClassifier<TClass, TInput> OpenMultilayerNetworkClassifier<TInput, TClass>(
            this IBlobStore store, string key, IFloatingPointFeatureExtractor<TInput> featureExtractor = null) where TInput : class where TClass : IEquatable<TClass>
        {
            if (featureExtractor == null) featureExtractor = new FeatureProcessingPipline<TInput>().FeatureExtractor;

            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(featureExtractor);

            store.Restore(key, classifier);

            return classifier;
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