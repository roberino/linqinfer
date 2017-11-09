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
            int i = 0;

            bool halted = false;

            double lastError = 0;

            foreach (var batchTask in trainingData.ExtractInputOutputIVectorBatches())
            {
                var batch = await batchTask;

                _learningProcessor.Train(batch, (n, e) =>
                {
                    halted = haltingFunction(n * (i + 1), e);

                    lastError = e;

                    return halted;
                });

                if (halted) break;

                i++;
            }

            return lastError;
        }
    }
}
