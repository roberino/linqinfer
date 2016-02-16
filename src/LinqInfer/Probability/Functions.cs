using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Probability
{
    internal static class Functions
    {
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
        public static double NormalDistribution(float x, double theta, double mu)
        {
            double a = theta * Math.Sqrt(Math.PI * 2f);
            double b = Math.Pow(x - mu, 2);
            double c = 2 * Math.Pow(theta, 2);

            // (1 / a) * (Math.Pow(Math.E, -(b / c)));

            return (1 / a) * Math.Exp(-(b / c));

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

            //=0.1 + (G12 /(H12 - 1) * 0.8)

            return Enumerable.Range(0, bucketCount).Select(n => (double)n).Select(n => min + (n / bc * max)).ToArray();
        }
    }
}