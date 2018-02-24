using LinqInfer.Learning;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class HeirarchicalGraphBuilderTests
    {
        [Test]
        public async Task CreateBinaryGraph_RandomDataSet_CreatesBinaryGraphStructure()
        {
            var data = Functions.NormalRandomDataset(0.2, 0.4, 16)
                .Select(x => new
                {
                    a = x,
                    b = x < 0.4 ? 1 : 0
                })
                .AsAsyncEnumerator();

            var pipe = data.CreatePipeline(x => ColumnVector1D.Create(x.a, x.b), 2);

            var graph = await pipe.CreateBinaryGraphAsync();

            var gexf = await graph.ExportAsGexfAsync();
            
            using (var fs = File.OpenWrite(@"h.gexf"))
            {
                await gexf.SaveAsync(fs, System.Xml.Linq.SaveOptions.None, CancellationToken.None);
            }
        }
    }
}