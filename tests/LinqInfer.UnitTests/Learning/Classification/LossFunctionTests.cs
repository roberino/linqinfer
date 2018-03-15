using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class LossFunctionTests
    {
        [Test]
        public void CrossEntropy_WhenGivenSmallValue_ThenValidValueReturned()
        {
            var x0 = 0.00000000000016672616851129717d;
            var x1 = 1 - x0;
            var actual = new Vector(new[] { x0, x1 });
            var target = new OneOfNVector(2, 1);

            var ce = LossFunctions.CrossEntropy;

            var e = ce.Calculate(actual, target, x => 1);

            Assert.That(e.Loss, Is.GreaterThan(0));
            Assert.That(e.DerivativeError.Sum, Is.LessThan(0));
        }

        [Test]
        public void CrossEntropy_WhenOneOfNVector_ThenValidValueReturned()
        {
            var x0 = 0.1912d;
            var x1 = 1 - x0;
            var actual = new Vector(new[] { x0, x1 });
            var target = new OneOfNVector(2, 1);

            var ce = LossFunctions.CrossEntropy;

            var e = ce.Calculate(actual, target, x => 1);

            Assert.That(e.Loss, Is.GreaterThan(0));
            Assert.That(e.DerivativeError.Sum, Is.LessThan(0));
        }
    }
}