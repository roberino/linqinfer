using LinqInfer.Data.Pipes;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Data.Pipes
{
    [TestFixture]
    public class AsyncEnumerableToBatchAdapterTests
    {
        [Test]
        public async Task AsyncEnumerable_GivenData_CanProcessItems()
        {
            var data = RangeAsync(1, 100);

            var enumerableBatches = From.AsyncEnumerable(data, 10);

            var results = new List<int>();

            await enumerableBatches.Filter(x => x % 2 == 0).ProcessUsing(x =>
            {
                results.AddRange(x.Items);

                return true;
            });

            Assert.That(results.Count, Is.EqualTo(50));
            Assert.That(results.Max(), Is.EqualTo(100));
            Assert.That(results.All(x => x % 2 == 0));
        }

        static async IAsyncEnumerable<int> RangeAsync(int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(1);
                yield return start + i;
            }
        }
    }
}
