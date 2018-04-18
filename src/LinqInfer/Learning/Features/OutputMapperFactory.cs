using LinqInfer.Data.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class OutputMapperFactory<TInput, TClass> where TClass : IEquatable<TClass>
    {
        public ICategoricalOutputMapper<TClass> Create(IEnumerable<TClass> outputs)
        {
            if (!outputs.Any()) throw new ArgumentException("No training data or classes found");

            return new OutputMapper<TClass>(outputs);
        }

        public ICategoricalOutputMapper<TClass> Create(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classifyingExpression)
        {
            var outputs = trainingData.GroupBy(classifyingExpression).Select(o => o.Key).ToList();

            return Create(outputs);
        }

        public async Task<ICategoricalOutputMapper<TClass>> CreateAsync<TInput2>(IAsyncFeatureProcessingPipeline<TInput2> trainingPipeline, Expression<Func<TInput, TClass>> classifyingExpression, CancellationToken cancellationToken, int maxSampleSize = 1000)
            where TInput2 : class, TInput
        {
            var trainingData = await trainingPipeline.Source.ToMemoryAsync(cancellationToken, maxSampleSize);
            var outputs = trainingData.Select(t => t.Value).AsQueryable().GroupBy(classifyingExpression).Select(o => o.Key).ToList();

            return Create(outputs);
        }
    }
}