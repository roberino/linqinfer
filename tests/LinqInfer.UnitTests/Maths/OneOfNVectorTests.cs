using LinqInfer.Maths;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class OneOfNVectorTests
    {
        [Test]
        public void Equals_ReturnTrueForColumnAndOneOfNVector()
        {
            var vect1 = new OneOfNVector(4, 3);

            var vect2 = vect1.ToColumnVector();

            Assert.That(vect1.Equals(vect2));
            Assert.That(vect2.Equals(vect1));
        }

        [Test]
        public void Equals_ReturnTrueForEqualOneOfNVectors()
        {
            var vect1 = new OneOfNVector(4, 3);
            var vect2 = new OneOfNVector(4, 3);

            Assert.That(vect1.Equals(vect2));
            Assert.That(vect2.Equals(vect1));
        }

        [Test]
        public void Equals_ReturnFalseForNonEqualOneOfNVectors()
        {
            var vect1 = new OneOfNVector(4, 3);
            var vect2 = new OneOfNVector(4, 2);

            Assert.That(vect1.Equals(vect2), Is.False);
        }

        [Test]
        public void Multiply_TwoOneOfNVectorsOfDifferentValues_ReturnsZeroVector()
        {
            var vect1 = new OneOfNVector(4, 3);
            var vect2 = new OneOfNVector(4, 2);

            var result = vect1.MultiplyBy(vect2);

            Assert.That(result, Is.InstanceOf<ZeroVector>());
            Assert.That(result.Size, Is.EqualTo(4));
            Assert.That(result.ToColumnVector().All(v => v == 0));
        }

        [Test]
        public void Multiply_TwoOneOfNVectorsOfSameValue_ReturnsSameVector()
        {
            var vect1 = new OneOfNVector(4, 2);
            var vect2 = new OneOfNVector(4, 2);

            var result = vect1.MultiplyBy(vect2);

            Assert.That(result, Is.InstanceOf<OneOfNVector>());
            Assert.That(result.Size, Is.EqualTo(4));
            Assert.That(result[1], Is.EqualTo(0));
            Assert.That(result[2], Is.EqualTo(1));
            Assert.That(result[3], Is.EqualTo(0));
            Assert.That(result[4], Is.EqualTo(0));
        }
    }
}