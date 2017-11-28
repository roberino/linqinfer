using LinqInfer.Maths;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MultiVectorTests
    {
        [Test]
        public void Construct_GivenMultipleVectorTypes_ReturnsTheCorrectProperties()
        {
            var vect = new MultiVector(
                new ColumnVector1D(1, 2, 3),
                new BitVector(true, false),
                new OneOfNVector(20, 4));

            Assert.That(vect.Size, Is.EqualTo(25));
            Assert.That(vect.InnerVectors.Count(), Is.EqualTo(3));

            Assert.That(vect[0], Is.EqualTo(1));
            Assert.That(vect[1], Is.EqualTo(2));
            Assert.That(vect[2], Is.EqualTo(3));

            Assert.That(vect[3], Is.EqualTo(1));
            Assert.That(vect[4], Is.EqualTo(0));

            foreach (var i in Enumerable.Range(5, 20).Where(n => n != 9))
            {
                Assert.That(vect[i], Is.EqualTo(0));
            }
        }

        [Test]
        public void DotProduct_GivenMultipleVectorTypesAndColumnVector_ReturnsCorrectValue()
        {
            var vect = new MultiVector(
                new ColumnVector1D(3, 6),
                new BitVector(true, false),
                new OneOfNVector(2, 1));

            var vect2 = new ColumnVector1D(2, 5, 8, 1, 5, 9);

            var x = vect.DotProduct(vect2);
        }

        [Test]
        public void ToByteArray_GivenMultipleVectorTypes_CanCreateEqualVectorFromBytes()
        {
            var vect = new MultiVector(
                new ColumnVector1D(1, 2, 3),
                new BitVector(true, false),
                new OneOfNVector(20, 4));

            var data = vect.ToByteArray();

            var vect2 = MultiVector.FromByteArray(data);

            Assert.That(vect2.Equals(vect));
        }

        [Test]
        public void ToString_GivenMultipleVectorTypes_ReturnsCsvRepresentation()
        {
            var vect = new MultiVector(
                new ColumnVector1D(1, 2.2, 3.23),
                new BitVector(true, false),
                new OneOfNVector(3, 1));

            var str = vect.ToString();

            var vals = str.Split(',').Select(v => double.Parse(v)).ToArray();

            Assert.That(vals.Length, Is.EqualTo(vect.Size));
        }

        [Test]
        public void GetHashCode_OverSimilarSamples_ReturnsZeroClashes()
        {
            var hashes = new List<int>();
            
            hashes.Add(new MultiVector(
                    new BitVector(true, false),
                    new OneOfNVector(2, 1)).GetHashCode());

            hashes.Add(new MultiVector(
                    new BitVector(true, false),
                    new OneOfNVector(2)).GetHashCode());

            hashes.Add(new MultiVector(
                    new BitVector(false, false),
                    new OneOfNVector(2)).GetHashCode());

            hashes.Add(new MultiVector(
                    new BitVector(false, true),
                    new OneOfNVector(2)).GetHashCode());

            hashes.Add(new MultiVector(
                    new BitVector(true, false),
                    new OneOfNVector(3)).GetHashCode());

            hashes.Add(new MultiVector(
                    new BitVector(true, false),
                    new OneOfNVector(2, 0)).GetHashCode());

            hashes.Add(new MultiVector(
                    new BitVector(true, false),
                    new OneOfNVector(2, 0),
                    new ColumnVector1D(1, 2)).GetHashCode());

            var uniquePercent = hashes.Distinct().Count() / (double)hashes.Count;

            Assert.That(uniquePercent, Is.EqualTo(1));
        }

        [Test]
        public void GetHashCode_OverRandomSamples_ReturnsMinimalClashes()
        {
            var hashes = new List<int>();

            foreach (var i in Enumerable.Range(0, 100))
            {
                var vect = new MultiVector(
                    new ColumnVector1D(1, Functions.RandomDouble(), 3.23),
                    new BitVector(true, false),
                    new OneOfNVector(15, Functions.Random(14)));

                hashes.Add(vect.GetHashCode());
            }

            var uniquePercent = hashes.Distinct().Count() / (double)hashes.Count;

            Assert.That(uniquePercent, Is.GreaterThan(0.9));
        }

        [Test]
        public void ToColumnVector_GivenMultipleVectorTypes_CanCreateEquivalentColVector()
        {
            var vect = new MultiVector(
                new ColumnVector1D(1, 2, 3),
                new BitVector(true, false),
                new OneOfNVector(3, 1));

            var colVect = vect.ToColumnVector();

            Assert.That(colVect[0], Is.EqualTo(1));
            Assert.That(colVect[1], Is.EqualTo(2));
            Assert.That(colVect[2], Is.EqualTo(3));

            Assert.That(colVect[3], Is.EqualTo(1));
            Assert.That(colVect[4], Is.EqualTo(0));

            Assert.That(colVect[5], Is.EqualTo(0));
            Assert.That(colVect[6], Is.EqualTo(1));
            Assert.That(colVect[7], Is.EqualTo(0));
        }
    }
}