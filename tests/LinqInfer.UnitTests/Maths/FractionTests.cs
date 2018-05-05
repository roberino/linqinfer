using System;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class FractionTests : AssertionHelper
    {
        [TestCase(0.2, 1, 5)]
        [TestCase(0.6666666666666, 2, 3)]
        [TestCase(0.5, 1, 2)]
        [TestCase(10.25, 41, 4)]
        [TestCase(0.8000000001, 4, 5)]
        public void ToApproximateRational_ArbFloatingPointValue_ReturnsEquivValue(double v, int n, int d)
        {
            var x = Fraction.ApproximateRational(v);

            Assert.That(x.Numerator, Is.EqualTo(n));
            Assert.That(x.Denominator, Is.EqualTo(d));
            TestFixtureBase.AssertEquiv(x, v);
        }

        [Test]
        public void ToApproximateRational_HighIterations()
        {
            var x = Fraction.ApproximateRational(0.857142857d, 16);

            Assert.That(x.Numerator, Is.EqualTo(6));
            Assert.That(x.Denominator, Is.EqualTo(7));
        }

        [TestCase(5)]
        [TestCase(7)]
        [TestCase(23)]
        // [TestCase(43)]
        [TestCase(23657)]
        public void ToApproximateRational_SqRt_ReturnsEquivValue(int n)
        {
            var y = Math.Sqrt(n);
            var x = Fraction.ApproximateRational(y, 8);

            TestFixtureBase.AssertEquiv(x, y, 4);
        }

        [Test]
        public void ToApproximateRational_PII_ReturnsEquivValue()
        {
            var x = Math.PI;
            var y = Fraction.ApproximateRational(x);

            Console.Write("{0}~={1} = {2}", x, y, y.Value);
            TestFixtureBase.AssertEquiv(y, x);
        }

        [TestCase(5, 1, 2)]
        [TestCase(15, 1, 3)]
        [TestCase(15, 2, 3)]
        [TestCase(9, 2, 3)]
        [TestCase(9, 2, 5)]
        public void Power_Fraction_IntegerExponent(int x, int n, int d)
        {
            var a = new Fraction(n, d);
            var pow = a.Power(x);
            var exp = Math.Pow((double)n / (double)d, x);

            Assert.That(Math.Round(pow.Value, 8), Is.EqualTo(Math.Round(exp, 8)));
        }

        [TestCase(5, 1, 2)]
        [TestCase(15, 1, 3)]
        [TestCase(15, 2, 3)]
        [TestCase(9, 2, 3)]
        [TestCase(9, 2, 5)]
        [TestCase(103, 2, 5)]
        //[TestCase(32, 10, 105)]
        public void Power_Integer(int x, int n, int d)
        {
            var y = Fraction.Power(x, n.OutOf(d), approx: true);
            var exp = Math.Pow(x, (double)n / (double)d);
            Assert.That(Math.Round(y.Value, 4), Is.EqualTo(Math.Round(exp, 4)));
        }

        [Test]
        public void RootOf_SqRootOf5()
        {
            var x = Fraction.RootOf(5, 2);
            Assert.That(Math.Round(x.Value, 6), Is.EqualTo(Math.Round(Math.Sqrt(5), 6)));
        }

        [TestCase(5, 3, 6)]
        [TestCase(5, 4, 6)]
        [TestCase(5, 2, 7)]
        [TestCase(509, 2, 6)]
        public void NthRootOfX(int x, int n, int p)
        {
            var v = Fraction.RootOf(x, n);
            Assert.That(Math.Round(v.Value, p), Is.EqualTo(Math.Round(Math.Pow(x, 1d / (double)n), p)));
        }

        [Test]
        public void E_ReturnsCorrectResult()
        {
            var e = Math.E;
            var ef = Fraction.E;

            Assert.That(Math.Round(ef.Value, 8), Is.EqualTo(Math.Round(e, 8)));
        }

        [TestCase(4, 5, 6)]
        [TestCase(4, 5, 8)]
        [TestCase(4, 5, 2)]
        [TestCase(8, 1, 5)]
        [TestCase(1, 8, 6)]
        [TestCase(1, 100, 6)]
        public void Sqrt(int n, int d, int p)
        {
            var sq = n.OutOf(d).Sqrt(p);
            var sq2 = Math.Sqrt((double)n / (double)d);

            Assert.AreEqual(Math.Round(sq.Value, p), Math.Round(sq2, p));
        }

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