using LinqInfer.Learning.Classification.NeuralNetworks;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class LossFunctionTests
    {
        [Test]
        public void CrossEntropy_WhenGivenSmallValue_ThenValidValueReturned()
        {
            var x = 0.00000000000016672616851129717d;

            var ce = LossFunctions.CrossEntropy;

            var e = ce.Calculate(x, 1);

            Assert.That(double.IsNaN(e.Loss), Is.False);
        }
    }
}