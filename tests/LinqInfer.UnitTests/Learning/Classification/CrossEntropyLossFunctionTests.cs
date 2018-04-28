using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class CrossEntropyLossFunctionTests
    {
        [Test]
        public void Calculate_WhenTwoVects_ThenErrorFromOneIsSmallerThanOther()
        {
            var func = new CrossEntropyLossFunction();

            var v1 = new ColumnVector1D(0.5, 0.6, 0.6);
            var v2 = new ColumnVector1D(0.99, -0.8, 0.6);
            var expected = new OneOfNVector(3, 1);

            var loss1 = func.Calculate(v1, expected, x => x);
            var loss2 = func.Calculate(v2, expected, x => x);

            Assert.That(loss1.DerivativeError[0], Is.LessThan(loss2.DerivativeError[0]));
            Assert.That(loss1.DerivativeError[1], Is.LessThan(loss2.DerivativeError[1]));
            Assert.That(loss1.DerivativeError[2], Is.EqualTo(loss2.DerivativeError[2]));
        }

        [Test]
        public void Calculate_WhenOneOfNVectorAndColVectCompared_ShouldReturnSameResult()
        {
            var func1 = new CrossEntropyLossFunction();
            var func2 = new CrossEntropyLossFunction();

            var v1 = new ColumnVector1D(0.99, -0.8, 0.6);
            var expected1 = new OneOfNVector(3, 1);
            var expected2 = new ColumnVector1D(0, 1, 0);

            var loss1 = func1.Calculate(v1, expected1, x => x);
            var loss2 = func2.Calculate(v1, expected2, x => x);

            Assert.That(loss1.DerivativeError.ToColumnVector().Equals(loss2.DerivativeError.ToColumnVector()));
        }
    }
}