using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    class LearningAsyncSinkAdapter : IAsyncSink<TrainingPair<IVector, IVector>>
    {
        readonly IAssistedLearningProcessor _processor;

        public LearningAsyncSinkAdapter(IAssistedLearningProcessor learningProcessor)
        {
            _processor = learningProcessor;
        }

        public double MinError { get; set; } = 0.2;

        public bool CanReceive => true;

        public Task ReceiveAsync(IBatch<TrainingPair<IVector, IVector>> dataBatch, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                _processor.Train(dataBatch.Items, (n, e) => e < MinError || cancellationToken.IsCancellationRequested);
            });
        }
    }
}