using LinqInfer.Utility;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Utility
{
    [TestFixture]
    public class AsyncEnumeratorTests
    {
        [Test]
        public async Task ProcessUsing_WhenGivenEnumerator_IteratesOverEachAwaitedBatch()
        {
            var asyncEnum = GetData().AsAsyncEnumerator();
            var results = new List<string>();

            int i = 0;

            var resultFlag = await asyncEnum.ProcessUsing(b =>
            {
                foreach (var s in b.Items) results.Add(s);

                Assert.That(b.BatchNumber, Is.EqualTo(i++));

                return true;
            });

            Assert.That(results.Count, Is.EqualTo(100));

            var expected = Enumerable.Range(0, 10).SelectMany(n => Enumerable.Range(0, 10).Select(m => $"{n},{m}"));

            Assert.That(results.Zip(expected, (s1, s2) => s1 == s2).All(x => x));
        }

        private IEnumerable<Task<IList<string>>> GetData()
        {
            foreach (var n in Enumerable.Range(0, 10))
            {
                yield return Task.Factory.StartNew<IList<string>>(() =>
                {
                    return Enumerable.Range(0, 10).Select(m => $"{n},{m}").ToList();
                });
            }
        }
    }
}