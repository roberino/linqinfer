using System;
using LinqInfer.Learning;
using LinqInfer.Utility;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class LstmExtensionsTests
    {
        [Test]
        public async Task AttachLongShortTermMemoryNetwork_SimpleSequence_ReturnsValidClassifier()
        {
            var data = Enumerable.Range('a', 10)
                .Concat(Enumerable.Range('c', 10))
                .Select(c => (char)c)
                .AsAsyncEnumerator();

            var trainingSet = await data.CreateCategoricalTimeSequenceTrainingSetAsync();

            var classifier = trainingSet.AttachLongShortTermMemoryNetwork();

            await trainingSet.RunAsync(CancellationToken.None);

            var results = classifier.Classify('e');

            Assert.That(results.First().ClassType, Is.EqualTo('f'));
        }
    }
}