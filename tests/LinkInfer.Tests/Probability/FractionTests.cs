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
    }
}