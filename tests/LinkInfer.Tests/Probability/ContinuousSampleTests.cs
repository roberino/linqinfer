using LinqInfer.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class ContinuousSampleTests
    {
        [Test]
        public void ProbabilityOf_LinearSample()
        {
            var kde = new KernelDensityEstimator();
            var data = Enumerable.Range(1, 100).Select(n => (n).OutOf(100)).AsQueryable();
            var sample = new ContinuousSample<Fraction>(data, kde);

            foreach(var x in Enumerable.Range(1, 100))
            {
                Console.WriteLine("{0}\t{1}", x, sample.ProbabilityOf(x.OutOf(100)).Value);
            }
        }

        [Test]
        public void ProbabilityOf_RandomSample()
        {
            var kde = new KernelDensityEstimator();
            var data = Enumerable.Range(1, 50).Select(n => Fraction.Random()).AsQueryable();
            var sample = new ContinuousSample<Fraction>(data, kde);

            foreach (var x in Enumerable.Range(1, 100))
            {
                Console.WriteLine("{0}\t{1}", x, sample.ProbabilityOf(x.OutOf(100)).Value);
            }
        }
    }
}
