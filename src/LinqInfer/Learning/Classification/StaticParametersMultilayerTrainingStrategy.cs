using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Classification
{
    internal class StaticParametersMultilayerTrainingStrategy<TClass, TInput> : IMultilayerNetworkTrainingStrategy<TClass, TInput> where TClass : IEquatable<TClass> where TInput : class
    {
        private readonly NetworkParameters _parameters;

        public StaticParametersMultilayerTrainingStrategy(NetworkParameters parameters)
        {
            _parameters = parameters;

            ErrorTolerance = 0.01f;
        }

        public float ErrorTolerance { get; set; }

        public IClassifierTrainingContext<TClass, NetworkParameters> Train(IFeatureProcessingPipeline<TInput> featureSet, Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory, Expression<Func<TInput, TClass>> classifyingExpression, ICategoricalOutputMapper<TClass> outputMapper)
        {
            var context = trainingContextFactory(_parameters);
            var classf = classifyingExpression.Compile();

            foreach (var batch in featureSet.ExtractBatches())
            {
                foreach (var value in batch.RandomOrder())
                {
                    context.Train(classf(value.Value), value.Vector);

                    if (context.AverageError.HasValue && context.AverageError < ErrorTolerance)
                    {
                        return context;
                    }
                }
            }

            return context;
        }
    }
}
