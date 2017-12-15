using LinqInfer.Learning;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class HeirarchicalGraphBuilderTests
    {
        [Test]
        [Ignore("WIP")]
        public async Task CreateBinaryGraph_RandomDataSet_CreatesBinaryGraphStructure()
        {
            var data = Functions.NormalRandomDataset(0.2, 0.4, 100)
                .Select(x => new
                {
                    a = x,
                    b = x < 0.4 ? 1 : 0
                })
                .AsAsyncEnumerator();

            var pipe = data.CreatePipeine(x => ColumnVector1D.Create(x.a, x.b), 2);

            var graph = await pipe.CreateBinaryGraphAsync(100);
        }
    }
}
