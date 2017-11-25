using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class VectorSerialiserTests
    {
        [Test]
        public void GivenColumnVector1DAndBase64_CanSerialiseAndDeserialise()
        {
            var sz = new VectorSerialiser();

            var vect = ColumnVector1D.Create(1, 2, 3, 4, double.Epsilon);

            var data = sz.Serialize(vect);

            var vect2 = sz.Deserialize(data);

            Assert.That(vect.Equals((ColumnVector1D)vect2));
        }

        [Test]
        public void GivenColumnVector1DAndCsv_CanSerialiseAndDeserialise()
        {
            var sz = new VectorSerialiser();

            var vect = ColumnVector1D.Create(1, 2, 3, 4, double.Epsilon);

            var data = sz.Serialize(vect, false);

            var vect2 = sz.Deserialize(data);

            Assert.That(vect.Equals((ColumnVector1D)vect2));
        }

        [Test]
        public void GivenOneOfNVector_CanSerialiseAndDeserialise()
        {
            var sz = new VectorSerialiser();

            var vect = new OneOfNVector(15, 12);

            var data = sz.Serialize(vect, false);

            var vect2 = sz.Deserialize(data);

            Assert.That(vect.Equals((OneOfNVector)vect2));
        }

        [Test]
        public void GivenBitVector_CanSerialiseAndDeserialise()
        {
            var sz = new VectorSerialiser();

            var vect = new BitVector(true, false, true);

            var data = sz.Serialize(vect, false);

            var vect2 = sz.Deserialize(data);

            Assert.That(vect.Equals((BitVector)vect2));
        }
    }
}