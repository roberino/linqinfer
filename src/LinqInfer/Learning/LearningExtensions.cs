﻿using LinqInfer.Learning.Features;
using LinqInfer.Learning.Nn;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    public static class LearningExtensions
    {
        private static readonly ObjectFeatureExtractor _ofo = new ObjectFeatureExtractor();

        /// <summary>
        /// Creates a self-organising feature map
        /// from an enumeration of objects.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="values">The values</param>
        /// <param name="normalisingSample">A sample which is used to normalise the data (provide an object which represents the maximum value for each parameter)</param>
        /// <param name="outputNodeCount">The maximum number of cluster nodes to output</param>
        /// <param name="normaliseData">True if the object vector should be normalised before processing</param>
        /// <param name="learningRate">The rate of learning</param>
        /// <returns>An enumeration of cluster nodes</returns>
        public static FeatureMap<T> ToSofm<T>(this IQueryable<T> values, T normalisingSample = null, int outputNodeCount = 10, bool normaliseData = true, float learningRate = 0.5f) where T : class
        {
            var fm = new FeatureMapper<T>(_ofo.CreateFeatureExtractor<T>(normaliseData), normalisingSample, outputNodeCount, learningRate);

            return fm.Map(values);
        }

        internal static FeatureMap<T> ToSofm<T>(this IQueryable<T> values, IFloatingPointFeatureExtractor<T> featureExtractor, int outputNodeCount = 10, float learningRate = 0.5f)
        {
            var fm = new FeatureMapper<T>(featureExtractor, default(T), outputNodeCount, learningRate);

            return fm.Map(values);
        }

        internal static FeatureMap<T> ToSofm<T>(this IQueryable<T> values, Func<T, float[]> featureExtractorFunc, string[] featureLabels = null, int outputNodeCount = 10, float learningRate = 0.5f)
        {
            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(featureExtractorFunc, featureExtractorFunc(default(T)).Length, false, featureLabels);

            var fm = new FeatureMapper<T>(featureExtractor, default(T), outputNodeCount, learningRate);

            return fm.Map(values);
        }

        /// <summary>
        /// Creates a function which, based on training data
        /// can classify a new object type and return a distribution
        /// of potential class matches.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="trainingData">The training data set</param>
        /// <param name="classf">A function which will be used to classify the training data</param>
        /// <returns>A function which can classify new objects, returning a dictionary of potential results</returns>
        public static Func<TInput, IDictionary<TClass, Fraction>> ToSimpleDistributionFunction<TInput, TClass>(this IQueryable<TInput> trainingData, Func<TInput, TClass> classf) where TInput : class
        {
            var extractor = _ofo.CreateFeatureExtractor<TInput>();
            var net = new SimpleNet<TClass>(extractor.VectorSize);
            var classifierPipe = new ClassificationPipeline<TClass, TInput, float>(net, net, extractor);

            classifierPipe.Train(trainingData, classf);

            return x =>
            {
                var matches = classifierPipe.FindPossibleMatches(x).ToList();
                var factor = Math.Max(matches.Count, 100);
                var total = (int)Math.Round(matches.Sum(m => m.Score * factor), 0);
                var dist = matches.ToDictionary(m => m.ClassType, m => new Fraction((int)System.Math.Round(m.Score * factor, 0), total));
                return dist;
            };
        }

        /// <summary>
        /// Creates a simple classifier based on some training data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="trainingData">The training data set</param>
        /// <param name="classf">A function which will be used to classify the training data</param>
        /// <returns>A function which can classify new objects, returning the best match</returns>
        public static Func<TInput, ClassifyResult<TClass>> ToSimpleClassifier<TInput, TClass>(this IQueryable<TInput> trainingData, Func<TInput, TClass> classf) where TInput : class
        {
            var extractor = _ofo.CreateFeatureExtractor<TInput>();
            var net = new SimpleNet<TClass>(extractor.VectorSize);
            var classifierPipe = new ClassificationPipeline<TClass, TInput, float>(net, net, extractor);

            classifierPipe.Train(trainingData, classf);

            return classifierPipe.Classify;
        }
    }
}
