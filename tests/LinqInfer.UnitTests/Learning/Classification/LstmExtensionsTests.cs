using System;
using LinqInfer.Learning;
using LinqInfer.Utility;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class LstmExtensionsTests
    {
        [Test]
        public async Task AttachLongShortTermMemoryNetwork()
        {
            var pipeline = await Enumerable
                .Range('a', 26)
                .Concat(Enumerable
                    .Range('a', 26))
                .Select(n => (char) n)
                .AsAsyncEnumerator()
                .CreateCategoricalPipelineAsync();

            var lstm = pipeline.AttachLongShortTermMemoryNetwork();

            await lstm.Pipe.RunAsync(CancellationToken.None);

            Console.WriteLine(lstm.Output.ExportData().ExportAsXml());
        }
    }
}
