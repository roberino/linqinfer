using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
    internal class NaiveBayesNormalClassifierBuilder : IAsyncSink<TrainingPair<IVector, IVector>>
    {
        private readonly IDensityEstimationStrategy<IVector> _kde;
        private readonly IDictionary<IVector, IList<IVector>> _samples;

        public NaiveBayesNormalClassifierBuilder(IDensityEstimationStrategy<IVector> kde)
        {
            _kde = kde;
            _samples = new Dictionary<IVector, IList<IVector>>(); 
        }

        public Func<IVector, IVector> Build()
        {
            var outputModels = _samples.Select(s => new
            {
                output = s.Key,
                kde = _kde.Evaluate(s.Value.AsQueryable())
            })
            .ToList();

            return x => outputModels
                .Select(m => m
                    .output
                    .MultiplyBy(Vector.UniformVector(m.output.Size, m.kde(x).Value)))
                    .MeanOfEachDimension();
        }

        public Task ReceiveAsync(IBatch<TrainingPair<IVector, IVector>> dataBatch, CancellationToken cancellationToken)
        {
            foreach (var item in dataBatch.Items)
            {
                if (!_samples.TryGetValue(item.TargetOutput, out IList<IVector> classSamples))
                {
                    _samples[item.TargetOutput] = new List<IVector> { item.Input };
                }
                else
                {
                    classSamples.Add(item.Input);
                }
            }

            return Task.FromResult(0);
        }
    }
}