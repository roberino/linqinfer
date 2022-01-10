using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Data.Pipes
{
    [TestFixture]
    public class AsyncEnumerableAdapterTests
    {
        [Test]
        public void T()
        {

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
