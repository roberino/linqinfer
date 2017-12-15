﻿using System;

namespace LinqInfer.Utility
{
    internal static class ArgAssert
    {
        public static void AssertEquals<T>(T x, T y, string name) where T : struct
        {
            if (!x.Equals(y)) throw new ArgumentException(name);
        }

        public static void Assert(Func<bool> assertion, string name)
        {
            if (!assertion())
            {
                throw new ArgumentException(name);
            }
        }

        public static void AssertLessThan(int value, int limit, string name)
        {
            if (value >= limit)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        public static void AssertGreaterThanZero(int value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        public static double AssertGreaterThanZero(double value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        public static int AssertGreaterThanOrEqualToZero(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        public static T AssertNonNull<T>(T value, string name) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            return value;
        }

        public static T[] AssertNonEmpty<T>(T[] values, string name)
        {
            AssertNonNull(values, name);

            if (values.Length == 0)
            {
                throw new ArgumentException(name);
            }

            return values;
        }
    }
}