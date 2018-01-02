﻿using LinqInfer.Data;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning
{
    public static class MlnExtensions
    {
        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="trainingSet">A asyncronous training set</param>
        public static IDynamicClassifier<TClass, TInput> AttachMultilayerNetworkClassifier<TInput, TClass>(
            this IAsyncTrainingSet<TInput, TClass> trainingSet,
            params int[] hiddenLayerSizes) where TInput : class where TClass : IEquatable<TClass>
        {
            var parameters = NetworkParameters.Sigmoidal(new[] { trainingSet.FeaturePipeline.FeatureExtractor.VectorSize }.Concat(hiddenLayerSizes).Concat(new[] { trainingSet.OutputMapper.VectorSize }).ToArray());
            var sink = new MultilayerNetworkAsyncSink<TInput, TClass>(parameters, (n, e) => true);
            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(trainingSet.FeaturePipeline.FeatureExtractor, trainingSet.OutputMapper, (MultilayerNetwork)sink.Classifier);

            trainingSet.RegisterSinks(sink);

            return classifier;
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
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this IFeatureProcessingPipeline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            float errorTolerance = 0.1f,
            Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<NetworkParameters>, double> fitnessFunction = null,
            Func<IClassifierTrainingContext<NetworkParameters>, int, TimeSpan, bool> haltingFunction = null) where TInput : class where TClass : IEquatable<TClass>
        {
            var defaultStrategy = new MaximumFitnessMultilayerNetworkTrainingStrategy<TClass, TInput>(errorTolerance, fitnessFunction, haltingFunction);

            return ToMultilayerNetworkClassifier(pipeline, classf, defaultStrategy);
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="trainingSet">A set of training data</param>
        /// <param name="errorTolerance">The network error tolerance</param>
        /// <param name="fitnessFunction">An optional fitness function which is used to determine the best solution found during the training phase</param>
        /// <param name="haltingFunction">An optional halting function which halts training once a solution is found (or not) 
        /// - the function takes the current fittest network, an interation index, and the elapsed time as parameters and
        /// return a true to indicate the training should stop</param>
        /// <returns></returns>
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this ITrainingSet<TInput, TClass> trainingSet,
            float errorTolerance = 0.1f,
            Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<NetworkParameters>, double> fitnessFunction = null,
            Func<IClassifierTrainingContext<NetworkParameters>, int, TimeSpan, bool> haltingFunction = null) where TInput : class where TClass : IEquatable<TClass>
        {
            var defaultStrategy = new MaximumFitnessMultilayerNetworkTrainingStrategy<TClass, TInput>(errorTolerance, fitnessFunction, haltingFunction);

            return ToMultilayerNetworkClassifier(trainingSet, defaultStrategy);
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied training data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="trainingSet">A training set</param>
        /// <param name="hiddenLayers">The number of neurons in each respective hidden layer</param>
        /// <returns>An executable object which produces a classifier</returns>
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this ITrainingSet<TInput, TClass> trainingSet,
            params int[] hiddenLayers) where TInput : class where TClass : IEquatable<TClass>
        {
            return ToMultilayerNetworkClassifier(trainingSet, 0.1d, hiddenLayers);
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
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this ITrainingSet<TInput, TClass> trainingSet,
            double learningRate = 0.1d,
            params int[] hiddenLayers) where TInput : class where TClass : IEquatable<TClass>
        {
            var trainingPipline = new MultilayerNetworkTrainingRunner<TClass, TInput>(trainingSet);
            var pipeline = trainingSet.FeaturePipeline;
            var inputSize = pipeline.VectorSize;
            var outputSize = trainingPipline.OutputMapper.VectorSize;

            var parameters = NetworkParameters.Sigmoidal(new[] { inputSize }.Concat(hiddenLayers).Concat(new[] { outputSize }).ToArray());

            parameters.LearningRate = learningRate;

            parameters.Validate();

            var strategy = new StaticParametersMultilayerTrainingStrategy<TClass, TInput>(parameters);

            return pipeline.ProcessWith((p, n) =>
            {
                var result = trainingPipline.TrainUsing(strategy).Result;

                return result;
            });
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <param name="hiddenLayers">The number of neurons in each respective hidden layer</param>
        /// <returns>An executable object which produces a classifier</returns>
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this IFeatureProcessingPipeline<TInput> pipeline,
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
                return trainingPipline.TrainUsing(strategy).Result;
            });
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied training data and training strategy.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="trainingSet">A set of training data</param>
        /// <param name="trainingStrategy">A implementation of a multilayer network training strategy</param>
        /// <returns></returns>
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this ITrainingSet<TInput, TClass> trainingSet,
            IAsyncMultilayerNetworkTrainingStrategy<TClass, TInput> trainingStrategy) where TInput : class where TClass : IEquatable<TClass>
        {
            return trainingSet.FeaturePipeline.ProcessAsyncWith(async (p, n) =>
            {
                var trainingPipline = new MultilayerNetworkTrainingRunner<TClass, TInput>(trainingSet);
                var result = await trainingPipline.TrainUsing(trainingStrategy);

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
        public static ExecutionPipline<IDynamicClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this IFeatureProcessingPipeline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            IAsyncMultilayerNetworkTrainingStrategy<TClass, TInput> trainingStrategy) where TInput : class where TClass : IEquatable<TClass>
        {
            return pipeline.ProcessAsyncWith(async (p, n) =>
            {
                var trainingSet = new TrainingSet<TInput, TClass>(pipeline, classf);
                var trainingPipline = new MultilayerNetworkTrainingRunner<TClass, TInput>(trainingSet);

                return await trainingPipline.TrainUsing(trainingStrategy);
            });
        }

        /// <summary>
        /// Restores a previously saved multi-layer network classifier from a blob store.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="docData">An exported multilayer network</returns>
        /// <param name="featureExtractor">An optional feature extractor</param>
        public static IDynamicClassifier<TClass, TInput> OpenAsMultilayerNetworkClassifier<TInput, TClass>(
            this BinaryVectorDocument docData, IFloatingPointFeatureExtractor<TInput> featureExtractor = null) where TInput : class where TClass : IEquatable<TClass>
        {
            var fe = new MultiFunctionFeatureExtractor<TInput>(featureExtractor);

            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(fe);

            classifier.FromVectorDocument(docData);

            return classifier;
        }

        /// <summary>
        /// Restores a previously saved multi-layer network classifier from a blob store.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="docData">An exported multilayer network</returns>
        /// <param name="featureExtractorFunc">A feature extracting function</param>
        public static INetworkClassifier<TClass, TInput> OpenAsMultilayerNetworkClassifier<TInput, TClass>(
            this BinaryVectorDocument docData, Func<TInput, double[]> featureExtractorFunc, int vectorSize) where TInput : class where TClass : IEquatable<TClass>
        {
            var fe = new MultiFunctionFeatureExtractor<TInput>(new DelegatingFloatingPointFeatureExtractor<TInput>(featureExtractorFunc, vectorSize));

            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(fe);

            classifier.FromVectorDocument(docData);

            return classifier;
        }
    }
}
