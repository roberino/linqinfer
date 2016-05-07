using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class HistogramTests
    {
        [Test]
        public void Analyse_SimpleSample()
        {
            var hist = new Histogram(1);
            var sample = new[] { 1, 4, 4, 5, 5, 5, 6, 6, 8, 10 }.Select(n => n.OutOf(1)).ToList().AsQueryable();
            var histSample = hist.Analyse(sample);

            Assert.That(histSample.Min, Is.EqualTo(1));
            Assert.That(histSample.Total, Is.EqualTo(sample.Count()));
            Assert.That(histSample.Width, Is.EqualTo(1));
            Assert.That(histSample.Bins.Count, Is.EqualTo(10));
            Assert.That(histSample.Bins[0], Is.EqualTo(1));
            Assert.That(histSample.Bins[9], Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_RandomSample()
        {
            var hist = new Histogram();
            var sample = Enumerable.Range(1, 100).Select(n => Fraction.Random()).ToList().AsQueryable();
            var histF = hist.Evaluate(sample);

            var p = histF((1).OutOf(32));
        }

        [Test]
        public void Evaluate_NormalSample()
        {
            var sample = Enumerable
                .Range(1, 10)
                .Select(n => new { p = Functions.NormalDistribution(n, 2.87f, 5), n = n })
                .SelectMany(x => Enumerable.Range(1, (int)(x.p * 100)).Select(n => (x.n).OutOf(10)))
                .ToList()
                .AsQueryable();

            var hist = new Histogram(0.02f);
            var kde = new KernelDensityEstimator(0.02f);
            var histF = hist.Evaluate(sample);
            var kdeF = kde.Evaluate(sample);
            var stdDevMu = Functions.MeanStdDev(sample);

            Console.WriteLine("Mean\t{0}", stdDevMu.Item1);
            Console.WriteLine("StdDev\t{0}", stdDevMu.Item2);

            var hRes = Enumerable.Range(1, 10).Select(n => new { n = n, p = histF(n.OutOf(10)) });
            var kRes = Enumerable.Range(1, 10).Select(n => kdeF(n.OutOf(10))).Normalise().ToList();
            int i = 0;

            foreach (var x in hRes)
            {
                var kr = kRes[i++];
                Console.WriteLine("{0}\t{1}\t{2}", x.n, x.p.Value, kr.Value);
                Assert.That(Math.Round(x.p.Value, 4), Is.EqualTo(Math.Round(kr.Value, 4)));
            }
        }
    }
}