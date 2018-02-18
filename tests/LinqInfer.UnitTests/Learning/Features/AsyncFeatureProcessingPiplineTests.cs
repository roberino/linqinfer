using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning.Features
{
    [TestFixture]
    public class AsyncFeatureProcessingPiplineTests
    {
        [Test]
        public async Task ExtractBatches_WhenGivenADataLoadingFunction_ReturnsExpectedObjectsAndExtractedVectors()
        {
            var fe = new DelegatingFloatingPointFeatureExtractor<string>(x => Vector.FromCsv(x).GetUnderlyingArray(), 2, new[] { "a", "b" });

            var pipeline = new AsyncFeatureProcessingPipeline<string>(Loader().AsAsyncEnumerator(), fe);

            int n = 0;

            foreach(var batch in pipeline.ExtractBatches().Items)
            {
                var items = await batch;

                int m = 0;

                foreach(var item in items)
                {
                    Assert.That(item.Vector[0], Is.EqualTo(n));
                    Assert.That(item.Vector[1], Is.EqualTo(m));
                    Assert.That(item.Value, Is.EqualTo($"{n},{m}"));

                    m++;
                }

                n++;
            }
        }

        private IEnumerable<Task<IList<string>>> Loader()
        {
            return Enumerable.Range(0, 10)
                .Select(n => Task<IList<string>>.Factory.StartNew(() =>
                    Enumerable.Range(0, 5).Select(m => $"{n},{m}").ToList()
                ));
        }
    }
}