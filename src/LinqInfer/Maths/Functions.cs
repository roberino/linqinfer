using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    public static class Functions
    {
        private static readonly Random _random = new System.Random((int)DateTime.UtcNow.Ticks);

        /// <summary>
        /// Returns a random number between zero and max (inclusive).
        /// </summary>
        /// <param name="max">The maximum value</param>
        /// <returns>An integer</returns>
        public static int Random(int max = 100)
        {
            return _random.Next(max + 1);
        }

        /// <summary>
        /// Returns a random number between zero and max (exclusive).
        /// </summary>
        /// <param name="min">The minimum value inclusive</param>
        /// <param name="max">The maximum value exclusive</param>
        /// <returns>A double precision floating point number</returns>
        public static double RandomDouble(double min = 0, double max = 1)
        {
            var m = max - min;
            return min + _random.NextDouble() * m;
        }

        /// <summary>
        /// Returns a random number between zero and max (inclusive).
        /// </summary>
        /// <param name="size">The size of the vector</param>
        /// <param name="min">The minimum value inclusive</param>
        /// <param name="max">The maximum value exclusive</param>
        /// <returns>A vector containing random values</returns>
        public static ColumnVector1D RandomVector(int size, double min = 0, double max = 1)
        {
            var m = max - min;
            return new ColumnVector1D(Enumerable.Range(1, size).Select(n => min + (_random.NextDouble() * m)).ToArray());
        }

        /// <summary>
        /// Returns a random number between zero and max (inclusive).
        /// </summary>
        /// <param name="size">The size of the vector</param>
        /// <param name="range">The min and max range</param>
        /// <returns>A vector containing random values</returns>
        public static ColumnVector1D RandomVector(int size, Range range)
        {
            return new ColumnVector1D(Enumerable.Range(1, size).Select(n => range.Min + _random.NextDouble() * range.Size).ToArray());
        }

        /// <summary>
        /// Normalises the set of values.
        /// </summary>
        /// <param name="values">The values</param>
        /// <returns>The resultant sample</returns>
        public static IEnumerable<Fraction> Normalise(this IEnumerable<Fraction> values, bool approx = true)
        {
            var sum = Fraction.ApproximateRational(values.Select(x => x.Value).Sum());

            return values.Select(x => Fraction.Divide(x, sum, approx));
        }

        /// <summary>
        /// Sums up an enumeration of fractions.
        /// </summary>
        /// <param name="values">The values</param>
        /// <returns>The resultant sum</returns>
        public static Fraction Sum(this IEnumerable<Fraction> values)
        {
            Contract.Assert(values != null);

            var total = values.First();

            foreach (var v in values.Skip(1))
            {
                total += v;
            }

            return total;
        }

        /// <summary>
        /// Returns the sum of the values.
        /// </summary>
        /// <param name="values">The values</param>
        /// <returns>A new vector containing the resultant sum</returns>
        public static ColumnVector1D Sum(this IEnumerable<ColumnVector1D> values)
        {
            Contract.Assert(values != null);

            var total = values.First();

            foreach (var v in values.Skip(1))
            {
                total += v;
            }

            return total;
        }

        public static Func<ColumnVector1D, ColumnVector1D> CreateNormalisingFunction(this IEnumerable<ColumnVector1D> values)
        {
            var max = values.MaxOfEachDimension();

            return x => x / max;
        }

        public static IEnumerable<ColumnVector1D> NormaliseEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            var max = values.MaxOfEachDimension();

            foreach(var v in values)
            {
                yield return v / max;
            }
        }

        public static ColumnVector1D MaxOfEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            if (values.Any())
            {
                return new ColumnVector1D(Enumerable.Range(0, values.First().Size).Select(n => values.Select(v => v[n]).Max()).ToArray());
            }

            throw new ArgumentException();
        }

        public static ColumnVector1D MinOfEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            if (values.Any())
            {
                return new ColumnVector1D(Enumerable.Range(0, values.First().Size).Select(n => values.Select(v => v[n]).Min()).ToArray());
            }

            throw new ArgumentException();
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

            return new Tuple<double, double>(mu, System.Math.Sqrt(items.Sum(x => System.Math.Pow(x - mu, 2)) / items.Count()));
        }
        public static Tuple<double, double> MeanStdDev(this IEnumerable<float> items)
        {
            var mu = items.Mean();

            return new Tuple<double, double>(mu, System.Math.Sqrt(items.Sum(x => System.Math.Pow(x - mu, 2)) / items.Count()));
        }

        public static Tuple<double, double> MeanStdDev(this IEnumerable<double> items)
        {
            var mu = items.Mean();

            return new Tuple<double, double>(mu, System.Math.Sqrt(items.Sum(x => System.Math.Pow(x - mu, 2)) / items.Count()));
        }
        public static Tuple<double, double> MeanStdDev(this IEnumerable<byte> items)
        {
            var mu = items.Mean();

            return new Tuple<double, double>(mu, System.Math.Sqrt(items.Sum(x => System.Math.Pow(x - mu, 2)) / items.Count()));
        }

        public static double NormalDistribution(double x, double theta, double mu)
        {
            var parts = NormalDistributionDebug(x, theta, mu);
            return parts.Item1 * parts.Item5;
        }

        internal static Tuple<double, double> NormalConst(double theta)
        {
            double a = 1d / (theta * System.Math.Sqrt(System.Math.PI * 2d));
            double b = 2 * System.Math.Pow(theta, 2);

            return new Tuple<double, double>(a, b);
        }

        internal static Tuple<double, double, double, double, double> NormalDistributionDebug(double x, double theta, double mu)
        {
            var con = NormalConst(theta);
            double a = con.Item1;
            double b = con.Item2;
            double c = System.Math.Pow(x - mu, 2);
            double d = -(c / con.Item2);
            double e = System.Math.Exp(d);

            return new Tuple<double, double, double, double, double>(a, b, c, d, e);

            // 1 / (theta * SqrR(2 * Pi)) * e -((x - mu) ^ 2) / (2 * theta ^ 2)
        }

        internal static Func<ColumnVector1D, double> MultiVariateNormalKernel(IEnumerable<ColumnVector1D> sample, double bandwidth)
        {
            var h = bandwidth;
            var hSq2 = 2 * h * h;
            var n = (double)sample.Count();
            var a = 1d / n;

            return (x) =>
            {
                var b = 1d / System.Math.Pow(System.Math.PI * hSq2, x.Size / 2);
                var c = sample.Select(x0 => b * System.Math.Exp(-System.Math.Pow((x - x0).EuclideanLength, 2) / hSq2)).Sum();

                return a * c;
            };
        }

        internal static Tuple<Fraction, Fraction, Fraction, Fraction, Fraction> NormalDistributionDebug(Fraction x, Fraction theta, Fraction mu)
        {
            var a = Fraction.Divide(Fraction.One, Fraction.Multiply(theta, Fraction.ApproximateRational(System.Math.Sqrt(System.Math.PI * 2)), true).Approximate(), true);
            var b = Fraction.Multiply(theta.Sq(true), (2).OutOf(1), true);
            var c = Fraction.Add(x, -mu, true).Sq(true);
            var d = Fraction.Multiply(Fraction.Divide(c, b, true), (-1).OutOf(1), true);
            var e = Fraction.ApproximateRational(System.Math.Exp(d.Value));

            return new Tuple<Fraction, Fraction, Fraction, Fraction, Fraction>(a, b, c, d, e);
        }

        internal static Fraction NormalDistribution(Fraction x, Fraction theta, Fraction mu)
        {
            var parts = NormalDistributionDebug(x, theta, mu);

            return Fraction.Multiply(parts.Item1, parts.Item5, true);
            
            // 1 / (theta * SqrR(2 * Pi)) * e -((x - mu) ^ 2) / (2 * theta ^ 2)
        }

        public static Func<float, double> NormalPdf(double theta, double mu)
        {
            return x => NormalDistribution(x, theta, mu);
        }

        internal static Func<int, double> UniformPdf(double value) // ??
        {
            return x => value;
        }

        internal static Func<float, double> BinaryPdf(double value)
        {
            return x => value == x ? value : 0f;
        }

        internal static Func<float, double> AutoPdf(double theta, double mu)
        {
            return theta == 0 ? BinaryPdf(mu) : NormalPdf(theta, mu);
        }

        public static double[] PercentileRange(int bucketCount, double percentile = 0.9d)
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