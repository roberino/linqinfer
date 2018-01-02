using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Threading.Tasks;
using LinqInfer.Data;
using System.Threading;

namespace LinqInfer.Learning.Classification
{
    internal class AsyncTrainingAdapter<TInput, TClass, TProcessor>
        : IAsyncSink<TrainingPair<IVector, IVector>>
        where TInput : class
        where TClass : IEquatable<TClass>
        where TProcessor : IAssistedLearningProcessor
    {
        private readonly Func<int, double, bool> _haltingFunction;

        public AsyncTrainingAdapter(TProcessor learningProcessor, Func<int, double, bool> haltingFunction)
        {
            Processor = learningProcessor;
            _haltingFunction = haltingFunction;
        }

        public TProcessor Processor { get; }

        public Task ReceiveAsync(IBatch<TrainingPair<IVector, IVector>> dataBatch, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                Processor.Train(dataBatch.Items, (n, e) => _haltingFunction(n, e) || cancellationToken.IsCancellationRequested);
            });
        }
    }
}