using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Probability
{
    [DebuggerDisplay("{Numerator}/{Denominator}")]
    public struct Fraction : IEquatable<Fraction>
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

        public static Fraction Create(int n, int d)
        {
            return new Fraction(n, d);
        }

        public static Fraction One
        {
            get
            {
                return new Fraction(1, 1);
            }
        }

        public static Fraction Zero
        {
            get
            {
                return new Fraction(0, 0);
            }
        }

        public static Fraction Half
        {
            get
            {
                return new Fraction(1, 2);
            }
        }

        public static Fraction ApproxPii
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

        public static Fraction E
        {
            get
            {
                const int n = 260412269;
                const int d = 95800320;
                return new Fraction(n, d, false);
            }
        }

        public static Fraction[] FindCommonDenominator(params Fraction[] fractions)
        {
            if (fractions.Length < 2) return fractions;

            var fLast = fractions[0];

            foreach(var f in fractions.Skip(1))
            {
                fLast = fLast * f;
            }

            return fractions.Select(f => new Fraction(f.Numerator * (fLast.Denominator / f.Denominator), fLast.Denominator, false)).ToArray();
        }

        public bool IsProper
        {
            get
            {
                return Value >= 0 && Value <= 1;
            }
        }

        public int Numerator;

        public int Denominator;

        public double Value
        {
            get
            {
                if (Denominator == 0) return 0;

                return (double)Numerator / (double)Denominator;
            }
        }

        public bool IsZero
        {
            get
            {
                return Numerator == 0 || Denominator == 0;
            }
        }
        
        public override string ToString()
        {
            return string.Format("{0}/{1}", Numerator, Denominator);
        }

        public Fraction Invert()
        {
            return new Fraction(Denominator, Numerator);
        }

        public Fraction Compliment(int total = 1)
        {
            Contract.Assert(total >= 1);
            Contract.Assert(total > 1 || IsProper);
            return new Fraction(total, 1) - this;
        }

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

                if (Math.Round( Math.Sqrt(Value), precision) == Math.Round(res.Value, precision))
                {
                    break;
                }

                f = f * 10d;
            }

            return final;
        }

        public Fraction Sq(bool approx = false)
        {
            return Multiply(this, this, approx);
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

            //if (approx)
            //{
            //    const int maxIterations = 1000;

            //    while (n0 > maxIterations || d0 > maxIterations)
            //    {
            //        n0 = (n0 >> 2);
            //        d0 = (d0 >> 2);
            //    }
            //}

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

            //var t = new Fraction(Numerator, Denominator);

            //if (n > 0)
            //{
            //    for (int i = 1; i < n; i++)
            //    {
            //        t = Multiply(t, t, approx);
            //    }
            //}
            //else
            //{
            //    for (int i = n; i > 0; i--)
            //    {
            //        t = Divide(t, t, approx);
            //    }
            //}

            //return t;
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

        public static Fraction operator +(Fraction d1, Fraction d2)
        {
            // e.g  2/4 + 5/6
            // =    6/12 + 10/12
            // =    16/12

            var comd = CommonDenominator(d1.Denominator, d2.Denominator);
            var d1n1 = d1.Numerator * (comd / d1.Denominator);
            var d2n1 = d2.Numerator * (comd / d2.Denominator);

            return new Fraction(d1n1 + d2n1, comd, true);
        }

        public static Fraction operator -(Fraction d1, Fraction d2)
        {
            // e.g  2/4 + 5/6
            // =    6/12 + 10/12
            // =    16/12

            var comd = CommonDenominator(d1.Denominator, d2.Denominator);
            var d1n1 = d1.Numerator * (comd / d1.Denominator);
            var d2n1 = d2.Numerator * (comd / d2.Denominator);

            return new Fraction(d1n1 - d2n1, comd, true);
        }

        internal static Fraction Multiply(Fraction x, Fraction y, bool approx = false)
        {
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
            if (y.IsZero) throw new DivideByZeroException();

            return Multiply(x, new Fraction(y.Denominator, y.Numerator, true), approx);
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

        private static int CommonDenominator(int d1, int d2)
        {
            int dmin = Math.Min(d1, d2);
            int dmax = Math.Max(d1, d2);
            int d = dmax;

            if (dmin == 0) return dmax;
               
            while(!(d % dmin == 0 && d % dmax == 0))
            {
                d++;
            }

            return d;
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

        public override bool Equals(object obj)
        {
            if (!(obj is Fraction)) return false;

            return Equals((Fraction)obj);
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

        private static Tuple<int, int> Reduce(long n, long d)
        {
            if (n == d) return new Tuple<int, int>(1, 1);

            var lcd = LargestCommonDivisor(n, d);

            return new Tuple<int, int>((int)(n / lcd), (int)(d / lcd));
        }
    }
}
