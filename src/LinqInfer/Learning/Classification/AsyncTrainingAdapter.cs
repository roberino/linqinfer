using LinqInfer.Learning.Features;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
    internal class AsyncTrainingAdapter<TInput, TClass>
        where TInput : class
        where TClass : IEquatable<TClass>
    {
        private readonly IAssistedLearningProcessor _learningProcessor;

        public AsyncTrainingAdapter(IAssistedLearningProcessor learningProcessor)
        {
            _learningProcessor = learningProcessor;
        }

        public async Task<double> Train(
            IAsyncTrainingSet<TInput, TClass> trainingData,
            Func<int, double, bool> haltingFunction)
        {
            bool halted = false;

            double lastError = 0;

            await trainingData
                .ExtractInputOutputIVectorBatches()
                .ProcessUsing(b =>
                {
                    _learningProcessor.Train(b.Items, (n, e) =>
                    {
                        halted = haltingFunction(n * (b.BatchNumber + 1), e);

                        lastError = e;

                        return halted;
                    });

                    return !halted;
                });

            return lastError;
        }
    }
}