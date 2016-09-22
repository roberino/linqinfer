using LinqInfer.Learning.Features;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
    [Obsolete("Use IAsyncMultilayerNetworkTrainingStrategy")]
    public interface IMultilayerNetworkTrainingStrategy<TClass, TInput> where TClass : IEquatable<TClass> where TInput : class
    {
        /// <summary>
        /// Trains one or more classifiers finding the best solution / fit for the data
        /// </summary>
        /// <param name="featureSet">A feature processing pipeline of input data</param>
        /// <param name="trainingContextFactory">A factory function which can generate new training contexts</param>
        /// <param name="classifyingExpression">An expression which can classify an input object</param>
        /// <returns></returns>
        IClassifierTrainingContext<TClass, NetworkParameters> Train(IFeatureProcessingPipeline<TInput> featureSet, Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory, Expression<Func<TInput, TClass>> classifyingExpression, ICategoricalOutputMapper<TClass> outputMapper);
    }

    public interface IAsyncMultilayerNetworkTrainingStrategy<TClass, TInput> where TClass : IEquatable<TClass> where TInput : class
    {
        /// <summary>
        /// Trains one or more classifiers finding the best solution / fit for the data
        /// </summary>
        /// <param name="trainingSet">A set of training data</param>    
        /// <param name="trainingContextFactory">A factory function which can generate new training contexts</param>
        /// <returns></returns>
        Task<IClassifierTrainingContext<TClass, NetworkParameters>> Train(ITrainingSet<TInput, TClass> trainingSet, Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory);
    }
}