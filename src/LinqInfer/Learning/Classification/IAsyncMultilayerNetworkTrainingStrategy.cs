using LinqInfer.Learning.Features;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
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