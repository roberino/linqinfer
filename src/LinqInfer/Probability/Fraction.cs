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

        public static Fraction operator *(Fraction d1, Fraction d2)
        {
            // e.g  2/4 * 5/6
            // =    10/24
            // =    5/12

            return new Fraction(d1.Numerator * d2.Numerator, d1.Denominator * d2.Denominator, true);
        }

        public static Fraction operator *(Fraction d1, int n)
        {
            // e.g  2/4 * 6
            // =    1/2 * 6
            // =    3

            var d2 = new Fraction(n, 1);

            return new Fraction(d1.Numerator * d2.Numerator, d1.Denominator * d2.Denominator, true);
        }

        public static Fraction operator /(Fraction d1, Fraction d2)
        {
            // e.g  6/14 / 5/10
            // =    12/14
            // e.g  1/2 / 1/4
            // =    2/4 / 1/4
            // =    8/4
            // e.g  8/5 / 1/3
            // =    24/15 / 3/15
            // =    62/15 

            if (d2.IsZero) throw new DivideByZeroException();

            return d1 * new Fraction(d2.Denominator, d2.Numerator, true);
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

        public static int LargestCommonDivisor(int a, int b)
        {
            int t = 0;

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

        private static Tuple<int, int> Reduce(int n, int d)
        {
            if (n == d) return new Tuple<int, int>(1, 1);

            var lcd = LargestCommonDivisor(n, d);

            return new Tuple<int, int>(n / lcd, d / lcd);
        }
    }
}
