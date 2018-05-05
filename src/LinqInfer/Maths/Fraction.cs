using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.IO;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represent a fraction
    /// </summary>
    [DebuggerDisplay("{Numerator}/{Denominator}")]
    public struct Fraction : IEquatable<Fraction>, IEquatable<int>, IComparable<Fraction>, IJsonExportable
    {
        public Fraction(int n, int d, bool reduce = true)
        {
            if (reduce)
            {
                if (d != 0)
                {
                    var r = Reduce(n, d);

                    Numerator = r.Item1;
                    Denominator = r.Item2;
                }
                else
                {
                    Numerator = 0;
                    Denominator = 1;
                }
            }
            else
            {
                if (d == 0) throw new InvalidOperationException();

                Numerator = n;
                Denominator = d;
            }
        }

        /// <summary>
        /// Creates a new fraction
        /// </summary>
        public static Fraction Create(int n, int d)
        {
            return new Fraction(n, d);
        }

        /// <summary>
        /// The value 1
        /// </summary>
        public static Fraction One
        {
            get
            {
                return new Fraction(1, 1, false);
            }
        }

        /// <summary>
        /// The value 0
        /// </summary>
        public static Fraction Zero
        {
            get
            {
                return new Fraction(0, 0);
            }
        }

        /// <summary>
        /// The value 0.5
        /// </summary>
        public static Fraction Half
        {
            get
            {
                return new Fraction(1, 2);
            }
        }

        public static Fraction Random(int maxPrecision = 100, int min = 0)
        {
            Contract.Requires(min < maxPrecision);

            return new Fraction(min + Functions.Random(maxPrecision - min), maxPrecision, true);
        }

        internal static Fraction ApproxPii
        {
            get
            {
                return new Fraction(22, 7, false);
            }
        }

        //private static Lazy<Fraction> _e = new Lazy<Fraction>(() =>
        //{
        //    var x = One;
        //    long d = 1;
        //    long a = 1;

        //    while (d < int.MaxValue)
        //    {
        //        x = x + new Fraction(1, (int)d, false);
        //        a++;
        //        d = d * a;
        //    }

        //    return x;
        //});

        internal static Fraction E
        {
            get
            {
                const int n = 260412269;
                const int d = 95800320;
                return new Fraction(n, d, false);
            }
        }

        internal static Fraction[] FindCommonDenominator(params Fraction[] fractions)
        {
            if (fractions.Length < 2) return fractions;

            var fLast = fractions[0];

            foreach(var f in fractions.Skip(1))
            {
                fLast = fLast * f;
            }

            return fractions.Select(f => new Fraction(f.Numerator * (fLast.Denominator / f.Denominator), fLast.Denominator, false)).ToArray();
        }

        /// <summary>
        /// Returns true if the fraction is a proper number
        /// </summary>
        public bool IsProper
        {
            get
            {
                return Value >= 0 && Value <= 1;
            }
        }

        /// <summary>
        /// The numerator (top number)
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator (bottom number)
        /// </summary>
        public int Denominator;

        /// <summary>
        /// The value as a double precision floating point number
        /// </summary>
        public double Value
        {
            get
            {
                if (Denominator == 0) return 0;

                return (double)Numerator / (double)Denominator;
            }
        }

        /// <summary>
        /// Returns true if the fraction evaluates to 0
        /// </summary>
        public bool IsZero
        {
            get
            {
                return Numerator == 0 || Denominator == 0;
            }
        }
        
        public override string ToString()
        {
            if (IsZero) return "0";
            return string.Format("{0}/{1}", Numerator, Denominator);
        }

        /// <summary>
        /// Flips the denominator and numerator (e.g. 2/5 becomes 5/2)
        /// </summary>
        public Fraction Invert()
        {
            return new Fraction(Denominator, Numerator);
        }

        /// <summary>
        /// Returns the compliment value of a proper fraction (e.g. compliment of 2/5 = 3/5)
        /// </summary>
        public Fraction Compliment(int total = 1)
        {
            Contract.Requires(total >= 1);
            Contract.Requires(total > 1 || IsProper);
            return new Fraction(total, 1) - this;
        }


        /// <summary>
        /// Returns a new fraction with the numerator and denominator reduced to their simplest form.
        /// </summary>
        public Fraction Reduce()
        {
            return new Fraction(Numerator, Denominator, true);
        }

        internal Fraction Root(int n, int precision = 6)
        {
            //TODO: Clumbsy implementation

            Fraction final = new Fraction();
            var f = 1d;
            var nd = (double)n;

            while (f < float.MaxValue)
            {
                var sqD = Math.Pow(Denominator * f, 1d / nd);
                var sqN = Math.Pow(Numerator * f, 1d / nd);

                var res = new Fraction((int)Math.Round(sqN), (int)Math.Round(sqD));

                if (double.IsPositiveInfinity(res.Value)) break;

                final = res;

                if (Math.Round(Math.Pow(Value, 1d / nd), precision) == Math.Round(res.Value, precision))
                {
                    break;
                }

                f = f * 10d;
            }

            return final;
        }

        /// <summary>
        /// Rounds the fraction to the nearest whole percentage (e.g. x/100)
        /// </summary>
        /// <returns>A new fraction</returns>
        public Fraction ToPercent()
        {
            return new Fraction((int)Math.Round(Value * 100d, 0), 100, false);
        }

        /// <summary>
        /// Returns the square root as a new fraction
        /// </summary>
        public Fraction Sqrt(int precision = 6)
        {
            //TODO: Clumbsy implementation

            Fraction final = new Fraction();
            var f = 1d;

            while (f < float.MaxValue)
            {
                var sqD = Math.Sqrt(Denominator * f);
                var sqN = Math.Sqrt(Numerator * f);

                var res = new Fraction((int)Math.Round(sqN), (int)Math.Round(sqD));

                if (double.IsPositiveInfinity(res.Value)) break;

                final = res;

                if (Math.Round(Math.Sqrt(Value), precision) == Math.Round(res.Value, precision))
                {
                    break;
                }

                f = f * 10d;
            }

            return final;
        }

        /// <summary>
        /// Returns the fraction multiplied by itself as a new fraction
        /// </summary>
        /// <param name="approx">To approximate when dealing with large values to prevent overflow</param>
        public Fraction Sq(bool approx = false)
        {
            return Multiply(this, this, approx);
        }

        /// <summary>
        /// Returns an approximate representation of the current value
        /// </summary>
        /// <param name="precision"></param>
        /// <returns></returns>
        public Fraction Approximate(int precision = 4)
        {
            Fraction a = this;
            int i = 3;

            while (i < 9)
            {
                a = ApproximateRational(Value, i++);

                if (Math.Round(a.Value, precision) == Math.Round(Value, precision)) break;
            }

            return a;
        }


        /// <summary>
        /// Finds an approximate rational equivalent fraction given a floating point number
        /// </summary>
        /// <param name="x">The value</param>
        /// <param name="iterations">The number iterations to find a value</param>
        /// <returns>A new fraction</returns>
        public static Fraction ApproximateRational(double x, int iterations = 8)
        {
            if (x == 1) return One;
            if (x == 0) return Zero;

            var cf = new List<int>(iterations);
            double lastR = x;
            long lastPq = 0;
            int i;

            for (i = 0; i < iterations; i++)
            {
                if (i > 0)
                {
                    if (lastR - lastPq == 0) break;
                    lastR = 1 / (lastR - lastPq);
                }

                lastPq = (long)Math.Floor(lastR);

                if (lastPq > int.MaxValue)
                {
                    if (i == 0)
                        throw new OverflowException();
                    else
                        break;
                }
                cf.Add((int)lastPq);
            }

            var a = Zero;

            int c = cf.Count;

            while (true)
            {
                try
                {
                    for (i = c - 1; i > 0; i--)
                    {
                        var n = new Fraction(cf[i], 1, false);

                        if (a.IsZero) a = One / n;
                        else a = One / (n + a);
                    }

                    break;
                }
                catch (DivideByZeroException)
                {
                    if (i < 1) break;

                    c = i;
                    a = Zero;
                }
                catch (OverflowException)
                {
                    if (i < 1) break;

                    c = i;
                    a = Zero;
                }
            }

            a = new Fraction(cf[0], 1) + a;

            return a;
        }

        internal static Fraction RootOf(int x, int n)
        {
            return new Fraction(x, 1, false).Root(n);
        }

        internal static Fraction Power(int x, Fraction r, bool approx = false)
        {
            return new Fraction(x, 1, false).Root(r.Denominator).Power(r.Numerator, approx);
        }

        internal Fraction Power(Fraction other, bool approx = false)
        {
            var n0 = Numerator;
            var d0 = Denominator;

            var n = Power(n0, other, approx);
            var d = Power(d0, other, approx);

            return n / d;
        }

        internal Fraction Power(int n, bool approx = false)
        {
            if (n == 0) return One;

            var n1 = (long)Math.Pow(Numerator, n);
            var d1 = (long)Math.Pow(Denominator, n);

            while (n1 > int.MaxValue || d1 > int.MaxValue)
            {
                if (!approx) throw new OverflowException();

                n1 = (n1 >> 2);
                d1 = (d1 >> 2);
            }

            return new Fraction((int)n1, (int)d1);
        }

        public static bool operator ==(Fraction d1, Fraction d2)
        {
            var r1 = d1.Reduce();
            var r2 = d2.Reduce();

            return r1.Numerator == r2.Numerator && r1.Denominator == r2.Denominator;
        }

        public static bool operator !=(Fraction d1, Fraction d2)
        {
            var r1 = d1.Reduce();
            var r2 = d2.Reduce();

            return r1.Numerator != r2.Numerator || r1.Denominator != r2.Denominator;
        }

        public static Fraction operator +(Fraction x, Fraction y)
        {
            return Add(x, y);
        }

        public static Fraction operator -(Fraction d1, Fraction d2)
        {
            return d1 + new Fraction(-d2.Numerator, d2.Denominator);
        }

        public static Fraction operator +(int x, Fraction y)
        {
            return new Fraction(x, 1) + new Fraction(y.Numerator, y.Denominator);
        }

        public static Fraction operator -(int x, Fraction y)
        {
            return new Fraction(x, 1) + new Fraction(-y.Numerator, y.Denominator);
        }

        public static Fraction operator -(Fraction y)
        {
            return new Fraction(-y.Numerator, y.Denominator);
        }

        public static implicit operator double(Fraction x)
        {
            return x.Value;
        }

        public static implicit operator Fraction(Tuple<int, int> tuple)
        {
            return new Fraction(tuple.Item1, tuple.Item2);
        }

        internal static Fraction Multiply(Fraction x, Fraction y, bool approx = false)
        {
            if (x.IsZero || y.IsZero) return Zero;

            long n = (long)x.Numerator * (long)y.Numerator;
            long d = (long)x.Denominator * (long)y.Denominator;

            var lcd = LargestCommonDivisor(n, d);

            while ((n / lcd) > int.MaxValue || (d / lcd) > int.MaxValue)
            {
                if (!approx) throw new OverflowException();

                n = (n >> 2);
                d = (d >> 2);
            }

            int n0 = (int)(n / lcd);
            int d0 = (int)(d / lcd);

            return new Fraction(n0, d0, false);
        }

        internal static Fraction Divide(Fraction x, Fraction y, bool approx = false)
        {
            if (y.IsZero) throw new DivideByZeroException(string.Format("Attempt to divide {0} by zero", x));

            return Multiply(x, new Fraction(y.Denominator, y.Numerator, true), approx);
        }

        internal static Fraction Add(Fraction x, Fraction y, bool approx = false)
        {
            // e.g  2/4 + 5/6
            // =    6/12 + 10/12
            // =    16/12

            var cd = (long)x.Denominator * (long)y.Denominator;
            var xn1 = (long)x.Numerator * (long)y.Denominator;
            var yn1 = (long)y.Numerator * (long)x.Denominator;
            var n = xn1 + yn1;
            var lcd = LargestCommonDivisor(n, cd);

            n = n / lcd;
            cd = cd / lcd;

            if (n > int.MaxValue || cd > int.MaxValue)
            {
                if (!approx) throw new OverflowException();

                return ApproximateRational((double)n / (double)cd);
            }

            return new Fraction((int)n, (int)cd, false);
        }

        public static Fraction operator *(Fraction x, Fraction y)
        {
            // e.g  2/4 * 5/6
            // =    10/24
            // =    5/12

            return Multiply(x, y);
        }

        public static Fraction operator *(Fraction x, int n)
        {
            // e.g  2/4 * 6
            // =    1/2 * 6
            // =    3

            var y = new Fraction(n, 1);
            return Multiply(x, y);
        }

        public static Fraction operator /(Fraction x, Fraction y)
        {
            // e.g  6/14 / 5/10
            // =    12/14
            // e.g  1/2 / 1/4
            // =    2/4 / 1/4
            // =    8/4
            // e.g  8/5 / 1/3
            // =    24/15 / 3/15
            // =    62/15 

            return Divide(x, y);
        }

        public static Fraction operator /(Fraction d1, int d)
        {
            return d1 / new Fraction(d, 1);
        }

        //function gcd(a, b)
        //    while b ≠ 0
        //       t := b; 
        //       b := a mod b; 
        //       a := t; 
        //    return a;

        public static long LargestCommonDivisor(long a, long b)
        {
            long t = 0;

            while(b != 0)
            {
                t = b;
                b = a % b;
                a = t;
            }

            return a;
        }

        public void WriteJson(TextWriter output)
        {
            Contract.Ensures(output != null);

            output.Write("{ denominator: " + Denominator + ", numerator: " + Numerator + " }");
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fraction)) return false;

            return Equals((Fraction)obj);
        }
        public bool Equals(int x)
        {
            return Equals(new Fraction(x, 1));
        }

        public bool Equals(int n, int d)
        {
            return Equals(new Fraction(n, d));
        }

        public bool Equals(Fraction other)
        {
            var a = other.Reduce();
            var b = Reduce();

            return a.Numerator == b.Numerator && a.Denominator == b.Denominator;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public int CompareTo(Fraction other)
        {
            return Value.CompareTo(other.Value);
        }

        private static Tuple<int, int> Reduce(long n, long d)
        {
            if (n == d) return new Tuple<int, int>(1, 1);

            var lcd = LargestCommonDivisor(n, d);

            return new Tuple<int, int>((int)(n / lcd), (int)(d / lcd));
        }
    }
}