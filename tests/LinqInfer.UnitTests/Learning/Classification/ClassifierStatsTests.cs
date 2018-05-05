using System.Linq;
using LinqInfer.Learning.Classification;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class ClassifierStatsTests
    {
        [Test]
        public void ToVectorDocument_SerialisesAsExpected()
        {
            var stats = new ClassifierStats();

            foreach(var n in Enumerable.Range(0, 17))
            {
                stats.IncrementClassificationCount();
                stats.IncrementTrainingSampleCount();
            }

            foreach (var n in Enumerable.Range(0, 5))
            {
                stats.IncrementTrainingSampleCount();
            }

            var doc = stats.ToDataDocument();

            Assert.That(doc.PropertyOrDefault("ClassificationCount", 0), Is.EqualTo(17));
            Assert.That(doc.PropertyOrDefault("TrainingSampleCount", 0), Is.EqualTo(22));
        }
    }
}
