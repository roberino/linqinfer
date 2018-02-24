using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class StaticParametersMultilayerTrainingStrategy<TClass, TInput> : IAsyncMultilayerNetworkTrainingStrategy<TClass, TInput> where TClass : IEquatable<TClass> where TInput : class
    {
        private readonly NetworkParameters _parameters;

        public StaticParametersMultilayerTrainingStrategy(NetworkParameters parameters)
        {
            _parameters = parameters;

            ErrorTolerance = 0.01f;
        }

        public float ErrorTolerance { get; set; }

        public Task<IClassifierTrainingContext<NetworkParameters>> Train(ITrainingSet<TInput, TClass> trainingSet, Func<NetworkParameters, IClassifierTrainingContext<NetworkParameters>> trainingContextFactory)
        {
            var context = trainingContextFactory(_parameters);

            return Task<IClassifierTrainingContext<NetworkParameters>>.Factory.StartNew(
                () =>
                {
                    foreach (var batch in trainingSet.ExtractTrainingVectorBatches())
                    {
                        foreach (var value in batch.RandomOrder())
                        {
                            context.Train(value.Input, value.TargetOutput);

                            if (context.AverageError.HasValue && context.AverageError < ErrorTolerance)
                            {
                                return context;
                            }
                        }
                    }

                    return context;
                });
        }
    }
}
