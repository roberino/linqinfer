using System.Linq;
using LinqInfer.Learning.Classification;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class CategoricalClassifierTests
    {
        [Test]
        public void TrainAndClassifier_ReturnsExpectedResult()
        {
            var classifier = new CategoricalClassifier<string>(4);

            classifier.Train("A", true, true, false, false);
            classifier.Train("A", true, false, false, false);
            classifier.Train("A", true, true, false, false);
            classifier.Train("B", false, true, true, false);
            classifier.Train("C", false, true, true, true);

            var results = classifier.Classify(true, true, false, false).ToList();
            var result = results.First();

            var prior = 3d / 5d;
            var posterier = (1 + 3d) * (1 + 2d);
            var probability = prior * posterier; // non-normal probability

            Assert.That(result.ClassType, Is.EqualTo("A"));
            Assert.That(result.Score, Is.EqualTo(probability));
        }
    }
}
