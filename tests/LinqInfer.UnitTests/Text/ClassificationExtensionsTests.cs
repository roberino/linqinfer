using LinqInfer.Learning;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class ClassificationExtensionsTests
    {
        [Test]
        public async Task CreateTimeSequenceTrainingSet_ExportData_CanOpenAsTextClassifier()
        {
            var corpus = TestData.CreateCorpus();

            var semanticSet = await corpus.ExtractKeyTermsAsync(CancellationToken.None);

            var trainingSet = corpus.CreateTimeSequenceTrainingSet(semanticSet);

            var lstm = trainingSet.AttachLongShortTermMemoryNetwork();
            
            var data = lstm.ExportData();

            var classifier = data.OpenTextClassifier();

            Assert.That(classifier, Is.Not.Null);
        }
    }
}
