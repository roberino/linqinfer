using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
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

        public Task<IClassifierTrainingContext<TClass, NetworkParameters>> Train(ITrainingSet<TInput, TClass> trainingSet, Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory)
        {
            var context = trainingContextFactory(_parameters);

            return Task<IClassifierTrainingContext<TClass, NetworkParameters>>.Factory.StartNew(
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
