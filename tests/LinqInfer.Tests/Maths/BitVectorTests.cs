using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    public class BitVectorTests
    {
        [Test]
        public void WhenConstructedWithASingleValueSmallArray_CreatesAValidInstance()
        {
            var vect = new BitVector(new[] { false, true, false });

            Assert.False(vect[0]);
            Assert.True(vect[1]);
            Assert.False(vect[2]);
        }

        [Test]
        public void WhenConstructedWithAMultiValueSmallArray_CreatesAValidInstance()
        {
            var vect = new BitVector(new[] { false, true, false, true, false });

            Assert.False(vect[0]);
            Assert.True(vect[1]);
            Assert.False(vect[2]);
            Assert.True(vect[3]);
            Assert.False(vect[4]);
        }
    }
}
