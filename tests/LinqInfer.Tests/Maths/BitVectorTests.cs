using LinqInfer.Maths;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    public class BitVectorTests
    {
        [Test]
        public void WhenConstructedWithASingleValueSmallArray_CreatesAValidInstance()
        {
            var vect = new BitVector(new[] { false, true, false });

            Assert.That(vect.Size, Is.EqualTo(3));
            Assert.False(vect.ValueAt(0));
            Assert.True(vect.ValueAt(1));
            Assert.False(vect.ValueAt(2));
        }

        [Test]
        public void WhenConstructedWithAMultiValueSmallArray_CreatesAValidInstance()
        {
            var vect = new BitVector(new[] { false, true, false, true, false });

            Assert.That(vect.Size, Is.EqualTo(5));
            Assert.False(vect.ValueAt(0));
            Assert.True(vect.ValueAt(1));
            Assert.False(vect.ValueAt(2));
            Assert.True(vect.ValueAt(3));
            Assert.False(vect.ValueAt(4));
        }

        [Test]
        public void WhenConstructedWithAMultiValueLargerArray_CreatesAValidInstance()
        {
            var values = new[] { false, true, false, true, false, true, true, false, true, true, false };
            var vect = new BitVector(values);

            Assert.That(vect.Size, Is.EqualTo(values.Length));

            int i = 0;

            foreach(var val in values)
            {
                Assert.That(vect.ValueAt(i++), Is.EqualTo(val));
            }
        }

        [Test]
        public void WhenMultipliedByAMatrix_ReturnsTheExpectedVector()
        {
            var matrix = new Matrix(Enumerable.Range(1, 3).Select(n => new double[] { n + 1, n - 1 }));
            var vect = new BitVector(new[] { true, false });

            // | 2, 0 |   | 1, 0 |
            // | 3, 1 | X
            // | 4, 2 |

            var result = matrix * vect;

            Assert.That(result.Size, Is.EqualTo(2));
            Assert.That(result[0], Is.EqualTo(9));
            Assert.That(result[1], Is.EqualTo(0));
        }

        [Test]
        public void ToBase64_WhenConvertedAndDeconverted_CreatesAnEqualInstance()
        {
            var values = Enumerable.Range(1, 20).Select(n => n % 3 == 0).ToArray();
            var vect = new BitVector(values);
            
            var data = vect.ToBase64();

            var vect2 = BitVector.FromBase64(data);

            Assert.That(vect2.Size, Is.EqualTo(values.Length));
        }

        [Test]
        public void Equals_TwoStrucurallyEquivVectors_ReturnsTrue()
        {
            var values = Enumerable.Range(1, 20).Select(n => n % 3 == 0);
            var vect1 = new BitVector(values.ToArray());
            var vect2 = new BitVector(values.ToArray());

            var result = vect1.Equals(vect2);

            Assert.That(result, Is.True);
        }
    }
}
