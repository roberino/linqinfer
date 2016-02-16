using System;
using System.Diagnostics.Contracts;

namespace LinqInfer.Probability
{
    public struct Fraction : IEquatable<Fraction>
    {
        public Fraction(int n, int d, bool reduce = true)
        {
            Contract.Assert(d != 0);

            if (reduce)
            {
                var r = Reduce(n, d);

                Numerator = r.Item1;
                Denominator = r.Item2;
            }
            else
            {
                Numerator = n;
                Denominator = d;
            }
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

        public int Numerator;

        public int Denominator;

        public float Value
        {
            get
            {
                return (float)Numerator / (float)Denominator;
            }
        }
        
        public override string ToString()
        {
            return string.Format("{0}/{1}", Numerator, Denominator);
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

            return d1 * new Fraction(d2.Denominator, d2.Numerator, true);
        }

        public static Fraction operator /(Fraction d1, int d)
        {
            return d1 / new Fraction(d, 1);
        }

        public static int CommonDenominator(int d1, int d2)
        {
            int dmin = Math.Min(d1, d2);
            int dmax = Math.Max(d1, d2);
            int d = dmax;
            
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

        private static Tuple<int, int> Reduce(int n, int d)
        {
            if (n == d) return new Tuple<int, int>(1, 1);

            var lcd = LargestCommonDivisor(n, d);

            return new Tuple<int, int>(n / lcd, d / lcd);
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
    }
}
