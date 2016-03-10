using LinqInfer.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class FunctionsTests
    {
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

            Assertions.AssertEquiv(p, expected, 2);
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

            Assertions.AssertEquiv(normal1.Item1, normal2.Item1, prec);
            Assertions.AssertEquiv(normal1.Item2, normal2.Item2, prec);
            Assertions.AssertEquiv(normal1.Item3, normal2.Item3, prec);
            Assertions.AssertEquiv(normal1.Item4, normal2.Item4, prec);
            Assertions.AssertEquiv(normal1.Item5, normal2.Item5, prec);
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

            Assertions.AssertEquiv(normal1, normal2, 3);
        }

        [Test]
        public void EnumerationOfFractions_MeanStdDev_ReturnsExpectedResults()
        {
            var v = new[] { 1, 8, 3, 12, 9 };
            var fracts = v.Select(x => new Fraction(1, x)).ToList();
            var floating = v.Select(x => 1d / (double)x).ToList();
            var meanAndStdDev = fracts.MeanStdDev();
            var meanAndStdDevF = floating.MeanStdDev();

            Assertions.AssertEquiv(meanAndStdDev.Item1, meanAndStdDevF.Item1);
            Assertions.AssertEquiv(meanAndStdDev.Item2, meanAndStdDevF.Item2);
        }
    }
}
