using LinqInfer.Data.Pipes;
using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Learning
{
    static class TestSamples
    {
        public static IAsyncFeatureProcessingPipeline<TestData.Pirate> CreatePipeline()
        {
            var pipeline = CreateLoader().CreatePipeline();

            return pipeline;
        }

        public static Func<int, AsyncBatch<TestData.Pirate>> CreateLoader() => Load;

        public static AsyncBatch<TestData.Pirate> Load(int n)
        {
            var items = Task.FromResult(
                (IList<TestData.Pirate>)Enumerable.Range(0, 10)
                    .Select(x => new TestData.Pirate()
                    {
                        Age = x,
                        Gold = n,
                        Ships = x * n,
                        IsCaptain = ((x * n) % 3) == 0,
                        Category = ((x * n) % 3) == 0 ? "a" : "b"
                    })
                    .ToList()
            );

            if (n > 9) throw new InvalidOperationException();

            return new AsyncBatch<TestData.Pirate>(items, n == 9, n);
        }
    }
}
