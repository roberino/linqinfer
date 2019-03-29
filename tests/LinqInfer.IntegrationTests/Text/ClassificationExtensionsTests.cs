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
        public void T()
        {
            var corpus = CorpusDataSource.GetCorpus(2500);
            var dict = new EnglishDictionary();

            foreach (var block in corpus.Blocks)
            {
                var workString = string.Join(',', dict.Encode(block.Select(t => t.Text)));

                Console.WriteLine(workString);
            }
        }

        [Test]
        public async Task CreateTextTimeSequenceTrainingSet()
        {
            DebugOutput.VerboseOn = false;

            var corpus = CorpusDataSource.GetCorpus(2500);
            var keyTerms = await corpus.ExtractAllTermsAsync();

            var trainingSet = corpus.CreateTimeSequenceTrainingSet(keyTerms);

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