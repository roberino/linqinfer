using LinqInfer.Learning;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class PipelineExtensionsTests
    {
        [Test]
        public void Filter_SaveState_RestoreState_PipelinesExtractDataTheSame()
        {
            var data = Enumerable.Range(1, 10).Select(n => new { x = n, y = 1 }).AsQueryable();
            var pipeline1 = data.CreatePipeline();
            var pipeline2 = data.CreatePipeline();

            pipeline1.FilterFeaturesByProperty(s => s.Select(p => p.x));

            var state = pipeline1.SaveState();

            pipeline2.RestoreState(state);

            var tranformedData1 = pipeline1.ExtractColumnVectors().ToList();
            var tranformedData2 = pipeline1.ExtractColumnVectors().ToList();

            Assert.That(tranformedData1.Zip(tranformedData2, (d1, d2) => d1.Equals(d2)).All(x => x));
        }

        [Test]
        public void Pcr_SaveState_RestoreState_PipelinesExtractDataTheSame()
        {
            var data = Enumerable.Range(1, 10).Select(n => new { x = n, y = n * 2, z = n * 3, a = n * 4 }).AsQueryable();
            var pipeline1 = data.CreatePipeline();
            var pipeline2 = data.CreatePipeline();

            pipeline1.PrincipalComponentReduction(2);

            var state = pipeline1.SaveState();

            Console.WriteLine(state.ExportAsXml());

            pipeline2.RestoreState(state);
            
            var tranformedData1 = pipeline1.ExtractColumnVectors().ToList();
            var tranformedData2 = pipeline1.ExtractColumnVectors().ToList();

            Assert.That(tranformedData1.Zip(tranformedData2, (d1, d2) => d1.Equals(d2)).All(x => x));
        }
    }
}
