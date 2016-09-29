using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class ColumnVector1DTests
    {
        [Test]
        public void Split_And_Concat()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3);
            var vect2 = ColumnVector1D.Create(4, 5);

            var vect1and2 = vect1.Concat(vect2);

            Assert.That(vect1and2.Size, Is.EqualTo(5));
            Assert.That(vect1and2[0], Is.EqualTo(1));
            Assert.That(vect1and2[1], Is.EqualTo(2));
            Assert.That(vect1and2[2], Is.EqualTo(3));
            Assert.That(vect1and2[3], Is.EqualTo(4));
            Assert.That(vect1and2[4], Is.EqualTo(5));

            var vects = vect1and2.Split(3);
            var vect1a = vects[0];
            var vect2a = vects[1];

            Assert.That(vect1a.Equals(vect1));
            Assert.That(vect2a.Equals(vect2));
        }

        [Test]
        public void Range_ReturnsCorrectValues()
        {
            var vect1 = ColumnVector1D.Create(2, 6, 8, 16);
            var vect2 = ColumnVector1D.Create(16, 16, 16, 16);
            var range = vect1.Range(vect2, 4).ToList();

            Assert.That(range.Count, Is.EqualTo(4));
            Assert.That(range.First(), Is.EqualTo(vect1));
            Assert.That(range.Last(), Is.EqualTo(vect2));
            Assert.That(range[1][0], Is.EqualTo(((16 - 2) / 3d) + 2));
            Assert.That(range[1][1], Is.EqualTo(((16 - 6) / 3d) + 6));
            Assert.That(range[1][2], Is.EqualTo(((16 - 8) / 3d) + 8));
            Assert.That(range[1][3], Is.EqualTo(16));

            foreach (var bin in range)
            {
                Console.WriteLine(bin.ToCsv(2));
            }
        }

        [Test]
        public void Equals_ReturnsTrueForSameValues()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3, 4);
            var vect2 = ColumnVector1D.Create(1, 2, 3, 4);

            Assert.That(vect1.Equals(vect2));
        }

        [Test]
        public void Equals_ReturnsFalseForDifferentValues()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3, 4);
            var vect2 = ColumnVector1D.Create(1, 3, 2, 4);

            Assert.That(vect1.Equals(vect2), Is.False);
        }
    }
}
