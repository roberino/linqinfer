using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MatrixTests : TestFixtureBase
    {
        [TestCase(10000, 2)]
        [TestCase(10000, 4)]
        [TestCase(10000, 8)]
        [TestCase(10000, 16)]
        [TestCase(10000, 32)]
        [TestCase(50000, 32)]
        public void Performance_Test(int height, int width)
        {
            var rnd1 = Functions.RandomGenerator(0.43, 4);
            var rnd2 = Functions.RandomGenerator(152, 12333.2);
            var vectors = Enumerable.Range(0, height).Select(r => ColumnVector1D.Create(Enumerable.Range(0, width).Select(c => c % 2 == 0 ? rnd1() : rnd2()).ToArray())).ToList();
            var sw = new Stopwatch();

            Console.WriteLine("Testing {0}x{1} matrix", width, height);

            sw.Start();

            var m = new Matrix(vectors);

            Console.WriteLine(sw.Elapsed);

            var covar = m.CovarianceMatrix;

            Console.WriteLine(sw.Elapsed);

            var m2 = covar * covar;

            Debug.Assert(m2 != null);

            Console.WriteLine(sw.Elapsed);
        }

        [Test]
        public void Performance_Test2()
        {
            var rnd1 = Functions.RandomGenerator(0.43, 4);
            var rnd2 = Functions.RandomGenerator(152, 12);
            var vectors1 = Enumerable.Range(0, 5000).Select(r => ColumnVector1D.Create(Enumerable.Range(0, 8).Select(c => c % 2 == 0 ? rnd1() : rnd2()).ToArray())).ToList();
            var vectors2 = Enumerable.Range(0, 8).Select(r => ColumnVector1D.Create(Enumerable.Range(0, 5000).Select(c => c % 2 == 0 ? rnd1() : rnd2()).ToArray())).ToList();

            Matrix a = null, b = null;

            TimeTest(() =>
            {
                a = new Matrix(vectors1);
                b = new Matrix(vectors2);

                Debug.Assert(a != null);
                Debug.Assert(b != null);
            }, "Setup");

            TimeTest(() =>
            {
                var m1 = a * b;
                Debug.Assert(m1 != null);

            }, "Multiply (operator)");


            TimeTest(() =>
            {
                var m2 = Matrix.Multiply(a, b);
                Debug.Assert(m2 != null);

            }, "Multiply (method)");
        }
    }
}