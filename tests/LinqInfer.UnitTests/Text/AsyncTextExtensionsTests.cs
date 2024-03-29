﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;
using LinqInfer.Text;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class AsyncTextExtensionsTests
    {
        static ISemanticSet dict = new SemanticSet(new HashSet<string>(new EnglishDictionary().Words.RandomOrder().Take(10)));

        [Test]
        public async Task BuildMultifunctionPipelineAsync_GivenAsyncData_ExtractsCorrectBatchCount()
        {
            var data = From.Func(Load);

            var pipeline = await data.BuildMultifunctionPipelineAsync(CancellationToken.None);

            var objectVectorPairs = await pipeline.ExtractBatches().ToMemoryAsync(CancellationToken.None);

            Assert.That(objectVectorPairs.Count, Is.EqualTo(100));

            Assert.That(objectVectorPairs.All(v => v.Vector.Size > 0));
        }

        static AsyncBatch<UnitTests.TestData.Pirate> Load(int n)
        {
            var items = Task.FromResult(
                    (IList<UnitTests.TestData.Pirate>)Enumerable.Range(0, 10)
                    .Select(x => new UnitTests.TestData.Pirate()
                    {
                        Age = x,
                        Gold = n,
                        Ships = x * n,
                        IsCaptain = ((x * n) % 3) == 0,
                        Category = ((x * n) % 3) == 0 ? "a" : "b",
                        Text = dict.RandomWord()
                    })
                    .ToList()
                    );

            if (n > 9) throw new InvalidOperationException();

            return new AsyncBatch<UnitTests.TestData.Pirate>(items, n == 9, n);
        }
    }
}
