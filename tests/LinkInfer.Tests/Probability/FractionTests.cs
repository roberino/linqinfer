using LinqInfer.Probability;
using NUnit.Framework;
using System;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class FractionTests
    {
        [Test]
        public void Math_Operations_ReturnExpected()
        {
            var f1 = new Fraction(1, 5);
            var f2 = new Fraction(4, 8);

            var f3 = f1 + f2;
            var f4 = f1 - f2;
            var f5 = f1 * f2;
            var f6 = f1 / f2;

            Console.WriteLine("{0} + {1} = {2}", f1, f2, f3);
            Console.WriteLine("{0} - {1} = {2}", f1, f2, f4);
            Console.WriteLine("{0} * {1} = {2}", f1, f2, f5);
            Console.WriteLine("{0} / {1} = {2}", f1, f2, f6);

            Assert.That(f3.Equals(7, 10));
            Assert.That(f4.Equals(-3, 10));
            Assert.That(f5.Equals(1, 10));
            Assert.That(f6.Equals(2, 5));
        }

        [TestCase(1, 2, 3, 4, 5, 4)]
        [TestCase(-1, 2, 3, 4, 1, 4)]
        [TestCase(0, 0, 3, 4, 3, 4)]
        public void Add_Operation_ReturnExpected(int n1, int d1, int n2, int d2, int ne, int de)
        {
            var x1 = n1.OutOf(d1);
            var x2 = n2.OutOf(d2);
            var expected = ne.OutOf(de);

            Assert.That(x1 + x2, Is.EqualTo(expected));
        }

        [TestCase(1, 2, 3, 4, -1, 4)]
        [TestCase(-1, 2, 3, 4, -5, 4)]
        [TestCase(0, 0, 3, 4, -3, 4)]
        public void Subtract_Operation_ReturnExpected(int n1, int d1, int n2, int d2, int ne, int de)
        {
            var x1 = n1.OutOf(d1);
            var x2 = n2.OutOf(d2);
            var expected = ne.OutOf(de);

            Assert.That(x1 - x2, Is.EqualTo(expected));
        }

        [TestCase(1, 2, 3, 4, 2, 3)]
        [TestCase(-1, 2, 3, 4, -2, 3)]
        [TestCase(0, 0, 3, 4, 0, 0)]
        public void Divide_Operation_ReturnExpected(int n1, int d1, int n2, int d2, int ne, int de)
        {
            var x1 = n1.OutOf(d1);
            var x2 = n2.OutOf(d2);
            var expected = ne.OutOf(de);
            var actual = x1 / x2;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Divide_By_Zero_ThrowsError()
        {
            Assert.Throws<DivideByZeroException>(() => { var x = Fraction.One / Fraction.Zero; });
            Assert.Throws<DivideByZeroException>(() => { var x = (5).OutOf(6) / (0).OutOf(0); });
        }

        [TestCase(1, 2, 3, 4, 3, 8)]
        [TestCase(-1, 2, 3, 4, -3, 8)]
        [TestCase(0, 0, 3, 4, 0, 0)]
        public void Multiply_Operation_ReturnExpected(int n1, int d1, int n2, int d2, int ne, int de)
        {
            var x1 = n1.OutOf(d1);
            var x2 = n2.OutOf(d2);
            var expected = ne.OutOf(de);
            var actual = x1 * x2;

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}