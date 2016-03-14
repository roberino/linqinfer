using LinqInfer.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    static class DensityEstimationTests
    {
        public static IList<Fraction> RunSampleTest<T>(IDensityEstimationStrategy<T> kde, Func<Fraction, T> converter)
        {
            var sample = Enumerable
                    .Range(1, 10)
                    .Select(n => new { p = Functions.NormalDistribution(n, 2.87f, 5), n = n })
                    .SelectMany(x => Enumerable.Range(1, (int)(x.p * 100)).Select(n => (x.n).OutOf(10)))
                    .ToList()
                    .AsQueryable();

            var kdeF = kde.Evaluate(sample.Select(converter).AsQueryable());
            var stdDevMu = Functions.MeanStdDev(sample);

            Console.WriteLine("Mean\t{0}", stdDevMu.Item1);
            Console.WriteLine("StdDev\t{0}", stdDevMu.Item2);

            var kRes = Enumerable.Range(1, 10).Select(n => kdeF(converter(n.OutOf(10)))).Normalise().ToList();

            int i = 0;

            foreach (var x in kRes)
            {
                i++;

                Console.WriteLine("{0}\t{1}", i, x.Value);
            }

            return kRes;
        }
    }
}
