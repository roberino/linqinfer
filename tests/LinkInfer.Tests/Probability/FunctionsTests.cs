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
