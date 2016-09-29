using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class FunctionsTests
    {
        [Ignore("Hangs when invoked with others?? Contract error? WTF??")]
        public void NormalRandomiser()
        {
            var data = Functions.NormalRandomDataset(0.6, 78, 1000).ToList();

            foreach (var d in data.GroupBy(r => Math.Round(r, 0)).OrderBy(d => d.Key))
            {
                Console.WriteLine("{0}\t{1}", d.Key, d.Count());
            }

            Assert.That(data.Average(), Is.GreaterThan(75));
            Assert.That(data.Average(), Is.LessThan(81));
        }


        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(5, 120)]
        [TestCase(10, 3628800)]
        public void Factorial_ReturnsCorrectResults(int n, long result)
        {
            var f = Functions.Factorial(n);
            Assert.That(f, Is.EqualTo(result));
        }

        [Test]
        public void BinomialPdf_Default_SumsToOneOverRange1To10()
        {
            var pdf = Functions.BinomialPdf();
            double t = 0;

            foreach (var n in Enumerable.Range(1, 10))
            {
                var p = pdf(n);
                t += p;

                Console.WriteLine("{0}\t{1}", n, p);
            }

            Assert.That((int)Math.Round(t, 2), Is.EqualTo(1));
        }

        [TestCase(20)]
        [TestCase(19)]
        [TestCase(15)]
        [TestCase(12)]
        [TestCase(5)]
        //[TestCase(1)]
        public void BinomialPdf_SumsToOneOverRange1ToX(int x)
        {
            var pdf = Functions.BinomialPdf(x);
            double t = 0;

            foreach (var n in Enumerable.Range(1, x))
            {
                var p = pdf(n);
                t += p;

                Console.WriteLine("{0}\t{1}", n, p);
            }

            Assert.That(pdf(2), Is.EqualTo(pdf(x - 2)));
            Assert.That((int)Math.Round(t, 0), Is.EqualTo(1));
        }

        [Test]
        public void MultinomialPdf_SingleTrial()
        {
            var pdf = Functions.MultinomialPdf(1, (1).OutOf(6), (2).OutOf(6), (3).OutOf(6));

            var p1 = pdf(new[] { 1, 0, 0 });
            var p2 = pdf(new[] { 0, 1, 0 });
            var p3 = pdf(new[] { 0, 0, 1 });

            var t = p1 + p2 + p3;
            
            Assert.That((int)Math.Round(t, 4), Is.EqualTo(1));
        }

        [TestCase(20)]
        [TestCase(19)]
        [TestCase(15)]
        [TestCase(12)]
        [TestCase(5)]
        public void MultinomialPdf_SumsToOneOverRange1ToX(int x)
        {
            var pdf = Functions.MultinomialPdf(x, (1).OutOf(3), (2).OutOf(3));
            double t = 0;

            foreach (var n in Enumerable.Range(1, x))
            {
                var p = pdf(new[] { n, x - n });
                t += p;

                Console.WriteLine("{0}\t{1}", n, p);
            }

            Assert.That((int)Math.Round(t, 0), Is.EqualTo(1));
        }

        [Test]
        public void Factorial_GreaterThan20_ThrowsException()
        {
            long lastF = 0;

            foreach(var n in Enumerable.Range(10, 10))
            {
                var f = Functions.Factorial(n);

                if (f <= lastF)
                {
                    Assert.Fail(n.ToString());
                }

                lastF = f;
            }

            Assert.Throws<ArgumentException>(() => Functions.Factorial(21));
        }

        [TestCase(0.1, 0.2, 0.1, true)]
        [TestCase(0.1, 0.2, 0.5, true)]
        [TestCase(3, 7, 2, true)]
        [TestCase(-0.3, 0.7, 0.2, true)]
        [TestCase(-0.3, 0.7, 0.2, false)]
        [TestCase(3, 7, 2, false)]
        public void Mutate_ReturnsValuesInExpectedRange(double a, double b, double variance, bool logarithmic)
        {
            var ave = (a + b) / 2;
            var min = ave - variance - 0.00001d;
            var max = ave + variance + 0.00001d;

            var range = Enumerable.Range(1, 500).Select(n => Functions.Mutate(a, b, variance, logarithmic));

            foreach (var r in range)
            {
                Console.WriteLine("min {0}, max {1}, actual {2}", min, max, r);

                //Assert.That(r, Is.AtLeast(min));
                //Assert.That(r, Is.AtMost(max));
                Assert.That(r, Is.InRange(min, max));
            }
        }

        [Test]
        public void Max_ColumnVector1D_ReturnsCorrectResult()
        {
            var vector1 = ColumnVector1D.Create(6, 2, 3);
            var vector2 = ColumnVector1D.Create(5, 5, 4);

            var max = new[] { vector1, vector2 }.MaxOfEachDimension();

            Assert.That(max[0], Is.EqualTo(6));
            Assert.That(max[1], Is.EqualTo(5));
            Assert.That(max[2], Is.EqualTo(4));
        }

        [Test]
        public void MultiVariateNormalKernel_ReturnsExpectedResults()
        {
            var vector1 = ColumnVector1D.Create(1, 2, 3, 3, 4, 4, 5, 5, 5, 6, 6, 7, 7, 8, 8, 9);
            var vector2 = ColumnVector1D.Create(1, 2, 3, 3, 4, 4, 5, 5, 5, 5, 6, 7, 7, 8, 8, 9);

            var normF = Functions.MultiVariateNormalKernel(new[] { vector1, vector2 }, 2);

            var p = normF(ColumnVector1D.Create(2, 2, 3, 3, 4, 5, 5, 5, 5, 6, 6, 7, 8, 8, 8, 9));

            Assert.That(p > 0);
            Assert.That(p < 9);
        }

        [TestCase(1, 2, 0.5d, 0.04714d, 8.46)]
        public void NormalDistribution_ReturnsExpectedResult(int n, int d, double mu, double stdDev, double expected)
        {
            var x = n.OutOf(d);
            var muf = Fraction.ApproximateRational(mu);
            var stdDevf = Fraction.ApproximateRational(stdDev);
            var p = Functions.NormalDistribution(x, stdDevf, muf);

            TestFixtureBase.AssertEquiv(p, expected, 2);
        }

        [TestCase(0.5d, 0.5d, 0.04714d, 8.46)]
        public void NormalPdf_ReturnsExpectedResult(double x, double mu, double stdDev, double expected)
        {
            var pdf = Functions.NormalPdf(stdDev, mu);

            var p = pdf(x);

            Assert.AreEqual(expected, Math.Round(p, 2));
        }

        [TestCase(0.5d, 0.04714d)]
        public void NormalRandomiser_ReturnsValidRange(double mu, double stdDev)
        {
            var rnd = Functions.NormalRandomiser(stdDev, mu);

            foreach(var r in Enumerable.Range(1, 1000))
            {
                var x = rnd();

                Assert.That(x >= 0);
                Assert.That(x <= mu * 2);
            }
        }

        [Test]
        public void EnumerationOfFractions_NormalDistribution_ReturnsExpectedComponents()
        {
            var a = (1).OutOf(5);
            var v = new[] { 1, 8, 3, 12, 9 };
            var fracts = v.Select(x => new Fraction(1, x)).ToList();
            var floating = v.Select(x => 1d / (double)x).ToList();

            var meanAndStdDev = fracts.MeanStdDev();
            var meanAndStdDevF = floating.MeanStdDev();

            var normal1 = Functions.NormalDistributionDebug(a, meanAndStdDev.Item2, meanAndStdDev.Item1);
            var normal2 = Functions.NormalDistributionDebug(a.Value, meanAndStdDevF.Item2, meanAndStdDevF.Item1);
            var prec = 3;

            TestFixtureBase.AssertEquiv(normal1.Item1, normal2.Item1, prec);
            TestFixtureBase.AssertEquiv(normal1.Item2, normal2.Item2, prec);
            TestFixtureBase.AssertEquiv(normal1.Item3, normal2.Item3, prec);
            TestFixtureBase.AssertEquiv(normal1.Item4, normal2.Item4, prec);
            TestFixtureBase.AssertEquiv(normal1.Item5, normal2.Item5, prec);
        }

        [Test]
        public void EnumerationOfFractions_NormalDistribution_ReturnsExpectedResults()
        {
            var a = (1).OutOf(5);
            var v = new[] { 1, 8, 3, 12, 9 };
            var fracts = v.Select(x => new Fraction(1, x)).ToList();
            var floating = v.Select(x => 1d / (double)x).ToList();

            var meanAndStdDev = fracts.MeanStdDev();
            var meanAndStdDevF = floating.MeanStdDev();

            var normal1 = Functions.NormalDistribution(a, meanAndStdDev.Item2, meanAndStdDev.Item1);
            var normal2 = Functions.NormalDistribution(a.Value, meanAndStdDevF.Item2, meanAndStdDevF.Item1);

            TestFixtureBase.AssertEquiv(normal1, normal2, 3);
        }

        [Test]
        public void EnumerationOfFractions_MeanStdDev_ReturnsExpectedResults()
        {
            var v = new[] { 1, 8, 3, 12, 9 };
            var fracts = v.Select(x => new Fraction(1, x)).ToList();
            var floating = v.Select(x => 1d / (double)x).ToList();
            var meanAndStdDev = fracts.MeanStdDev();
            var meanAndStdDevF = floating.MeanStdDev();

            TestFixtureBase.AssertEquiv(meanAndStdDev.Item1, meanAndStdDevF.Item1);
            TestFixtureBase.AssertEquiv(meanAndStdDev.Item2, meanAndStdDevF.Item2);
        }
    }
}
