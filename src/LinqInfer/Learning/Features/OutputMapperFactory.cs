using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
    }
}