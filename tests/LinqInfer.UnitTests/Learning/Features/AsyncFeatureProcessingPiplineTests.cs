using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Features
{
    [TestFixture]
    public class AsyncFeatureProcessingPipelineTests
    {
        [Test]
        public async Task ExtractBatches_WhenGivenADataLoadingFunction_ReturnsExpectedObjectsAndExtractedVectors()
        {
            var fe = new ExpressionFeatureExtractor<string>(x => Vector.FromCsv(x, ','), 2);

            var pipeline = new AsyncFeatureProcessingPipeline<string>(Loader().AsAsyncEnumerator(), fe);

            var n = 0;
            var m = 0;

            foreach (var item in await pipeline
                .ExtractBatches()
                .ToMemoryAsync(CancellationToken.None))
            {
                Assert.That(item.Vector[0], Is.EqualTo(m));
                Assert.That(item.Vector[1], Is.EqualTo(n));

                n++;

                if (n != 5) continue;

                m++;
                n = 0;
            }
        }

        static IEnumerable<Task<IList<string>>> Loader()
        {
            return Enumerable.Range(0, 10)
                .Select(n => Task<IList<string>>.Factory.StartNew(() =>
                    Enumerable.Range(0, 5).Select(m => $"{n},{m}").ToList()
                ));
        }
    }
}