using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LinqInfer.Learning.Features;

namespace LinqInfer.Learning.Classification.Remoting
{
    /// <summary>
    /// Represents a client which can remotely request classifier tasks
    /// </summary>
    public interface IRemoteClassifierTrainingClient
    {
        /// <summary>
        /// Sets the timeout for a data packet
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Asyncronously creates a new classifier
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="trainingSet">A training set</param>
        /// <param name="remoteSave">True if a classifier should be saved</param>
        /// <param name="name">The name of the classifier</param>
        /// <param name="errorTolerance">The error tolerance</param>
        /// <param name="hiddenLayers">The hidden layer specification</param>
        /// <returns>A task which returns a classifier instance</returns>
        Task<KeyValuePair<Uri, IObjectClassifier<TClass, TInput>>> CreateClassifier<TInput, TClass>(ITrainingSet<TInput, TClass> trainingSet, bool remoteSave = false, string name = null, float errorTolerance = 0.1F, params int[] hiddenLayers)
            where TInput : class
            where TClass : IEquatable<TClass>;

        /// <summary>
        /// Deletes a saved classifier
        /// </summary>
        /// <param name="uri">The remote uri of the classifier</param>
        /// <returns>A boolean indicating whether the classifier has been deleted</returns>
        Task<bool> Delete(Uri uri);

        /// <summary>
        /// Asyncronously restores a previously saved classifier
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="uri">The remote uri of the classifier</param>
        /// <param name="uninitialisedFeatureExtractor">A feature extractor which will be restored</param>
        /// <param name="exampleClass">An optional example class</param>
        /// <returns>A task which returns a classifier instance</returns>
        Task<IObjectClassifier<TClass, TInput>> RestoreClassifier<TInput, TClass>(Uri uri, IFloatingPointFeatureExtractor<TInput> uninitialisedFeatureExtractor = null, TClass exampleClass = default(TClass))
            where TInput : class
            where TClass : IEquatable<TClass>;
    }
}