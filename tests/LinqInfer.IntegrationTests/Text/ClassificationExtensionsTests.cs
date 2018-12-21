using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Learning;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.IntegrationTests.Text
{
    [TestFixture]
    public class ClassificationExtensionsTests
    {
        [Test]
        public async Task CreateTextTimeSequenceTrainingSet()
        {
            DebugOutput.VerboseOn = false;

            var dictionary = new EnglishDictionary();
            var corpus = CorpusDataSource.GetCorpus(1000);
            var keyTerms = await corpus.ExtractAllTermsAsync();
            // var keyTerms = corpus.ExtractKeyTermsAsync(CancellationToken.None);
            var trainingSet = corpus.CreateTextTimeSequenceTrainingSet(keyTerms);

            var lstm = trainingSet.AttachLongShortTermMemoryNetwork();

            await trainingSet.RunAsync(CancellationToken.None);

            var next = lstm.Classify("this").FirstOrDefault();

            for (var x = 0 ; x < 100; x++)
            {
                Console.Write(next.ClassType + " ");
                next = lstm.Classify(next.ClassType).FirstOrDefault();
            }
        }
    }
}