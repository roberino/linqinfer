using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class ContinuousSampleTests
    {
        [Test]
        [Category("Build-Omit")]
        public void ProbabilityOf_LinearSample()
        {
            var kde = new KernelDensityEstimator();
            var data = Enumerable.Range(1, 100).Select(n => (n).OutOf(100)).AsQueryable();
            var sample = new ContinuousSample<Fraction>(data, kde);

            foreach(var x in Enumerable.Range(1, 100))
            {
                Console.WriteLine("{0}\t{1}", x, sample.DensityOf(x.OutOf(100)).Value);
            }
        }

        [Test]
        [Category("Build-Omit")]
        public void ProbabilityOf_RandomSample()
        {            
            var kde = new KernelDensityEstimator();
            var data = Enumerable.Range(1, 50).Select(n => Fraction.Random(100, 1)).AsQueryable();
            var sample = new ContinuousSample<Fraction>(data, kde);

            foreach (var x in Enumerable.Range(1, 100))
            {
                try
                {
                    Console.WriteLine("{0}\t{1}", x, sample.DensityOf(x.OutOf(100)).Value);
                }
                catch (DivideByZeroException)
                {
                    Console.WriteLine("Divide by zero error: {0}", x);
                }
            }
        }
    }
}
