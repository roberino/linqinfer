using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Probability
{
    internal static class Functions
    {
        /// <summary>
        /// Sums up an enumeration of fractions.
        /// </summary>
        /// <param name="values">The values</param>
        /// <returns>The resultant sum</returns>
        public static Fraction Sum(this IEnumerable<Fraction> values)
        {
            Fraction total = values.First();

            foreach (var v in values.Skip(1))
            {
                total += v;
            }

            return total;
        }
        public static Fraction Mean(this IEnumerable<Fraction> items)
        {
            var sum = items.Sum();
            var count = items.Count();
            return sum / count;
        }
        public static double Mean(this IEnumerable<int> items)
        {
            return items.Average();
        }
        public static double Mean(this IEnumerable<double> items)
        {
            return items.Average();
        }
        public static double Mean(this IEnumerable<float> items)
        {
            return items.Average();
        }
        public static double Mean(this IEnumerable<byte> items)
        {
            return items.Average(b => b);
        }

        public static Tuple<Fraction, Fraction> MeanStdDev(this IEnumerable<Fraction> items)
        {
            var mu = items.Mean();
            var t = Fraction.Zero;

            foreach (var x in items)
            {
                t += (x - mu).Sq();
            }

            t = t / items.Count();

            return new Tuple<Fraction, Fraction>(mu, t.Sqrt());
        }

        public static Tuple<double, double> MeanStdDev(this IEnumerable<int> items)
        {
            var mu = items.Mean();

            return new Tuple<double, double>(mu, Math.Sqrt(items.Sum(x => Math.Pow(x - mu, 2)) / items.Count()));
        }
        public static Tuple<double, double> MeanStdDev(this IEnumerable<float> items)
        {
            var mu = items.Mean();

            return new Tuple<double, double>(mu, Math.Sqrt(items.Sum(x => Math.Pow(x - mu, 2)) / items.Count()));
        }

        public static Tuple<double, double> MeanStdDev(this IEnumerable<double> items)
        {
            var mu = items.Mean();

            return new Tuple<double, double>(mu, Math.Sqrt(items.Sum(x => Math.Pow(x - mu, 2)) / items.Count()));
        }
        public static Tuple<double, double> MeanStdDev(this IEnumerable<byte> items)
        {
            var mu = items.Mean();

            return new Tuple<double, double>(mu, Math.Sqrt(items.Sum(x => Math.Pow(x - mu, 2)) / items.Count()));
        }

        public static double NormalDistribution(double x, double theta, double mu)
        {
            var parts = NormalDistributionDebug(x, theta, mu);
            return (1 / parts.Item1) * parts.Item5;
        }

        public static Tuple<double, double, double, double, double> NormalDistributionDebug(double x, double theta, double mu)
        {
            double a = theta * Math.Sqrt(Math.PI * 2f);
            double b = Math.Pow(x - mu, 2);
            double c = 2 * Math.Pow(theta, 2);
            double d = -(b / c);
            double e = Math.Exp(d);
            // (1 / a) * (Math.Pow(Math.E, -(b / c)));

            return new Tuple<double, double, double, double, double>(a, b, c, d, e);

            // 1 / (theta * SqrR(2 * Pi)) * e -((x - mu) ^ 2) / (2 * theta ^ 2)
        }

        internal static Tuple<Fraction, Fraction, Fraction, Fraction, Fraction> NormalDistributionDebug(Fraction x, Fraction theta, Fraction mu)
        {
            var a = Fraction.Multiply(theta, (Fraction.ApproxPii * 2).Sqrt(), true);
            var b = (x - mu).Sq();
            var c = Fraction.Multiply(theta.Sq(true), (2).OutOf(1), true);
            var d = Fraction.Multiply(Fraction.Divide(b, c, true), (-1).OutOf(1), true);
            var e = new Fraction((int)(Math.Exp(d.Value) * 100000), 100000);
            var e2 = Fraction.E.Power(d, true);

            return new Tuple<Fraction, Fraction, Fraction, Fraction, Fraction>(a, b, c, d, e);
        }

        internal static Fraction NormalDistribution(Fraction x, Fraction theta, Fraction mu)
        {
            var parts = NormalDistributionDebug(x, theta, mu);

            // (1 / a) * (Math.Pow(Math.E, -(b / c)));

            return Fraction.Multiply(Fraction.Divide(Fraction.One, parts.Item1, true), parts.Item5, true);

            //return (Fraction.One / a) * Math.Exp(-(b / c));

            // 1 / (theta * SqrR(2 * Pi)) * e -((x - mu) ^ 2) / (2 * theta ^ 2)
        }
        public static Func<float, double> NormalPdf(double theta, double mu)
        {
            return x => NormalDistribution(x, theta, mu);
        }

        public static Func<int, double> UniformPdf(double value) // ??
        {
            return x => value;
        }

        public static Func<float, double> BinaryPdf(double value)
        {
            return x => value == x ? value : 0f;
        }

        public static Func<float, double> AutoPdf(double theta, double mu)
        {
            return theta == 0 ? BinaryPdf(mu) : NormalPdf(theta, mu);
        }

        public static double[] PercentileRange(int bucketCount, double percentile = 0.9f)
        {
            Contract.Assert(percentile >= 0 && percentile < 1);

            double min = 1 - percentile;
            double max = percentile - min;
            double bc = bucketCount - 1;

            //=0.1 + (a /(b - 1) * 0.8)

            return Enumerable.Range(0, bucketCount).Select(n => (double)n).Select(n => min + (n / bc * max)).ToArray();
        }
    }
}