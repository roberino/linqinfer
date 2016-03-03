using LinqInfer.Probability;
using NUnit.Framework;
using System;

namespace LinqInfer.Tests
{
    public static class Assertions
    {
        public static void AssertEquiv(Fraction x, double y, int precision = 6)
        {
            Console.WriteLine("{0}={1}~={2}", x, x.Value, y);
            Assert.That(Math.Round(x.Value, precision), Is.EqualTo(Math.Round(y, precision)));
        }
    }
}
