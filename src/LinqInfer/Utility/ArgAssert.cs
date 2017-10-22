using System;

namespace LinqInfer.Utility
{
    internal static class ArgAssert
    {
        public static void Assert(Func<bool> assertion, string name)
        {
            if (assertion())
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

        public static void AssertGreaterThanOrEqualToZero(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        public static void AssertNonNull<T>(T value, string name) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void AssertNonEmpty<T>(T[] values, string name)
        {
            AssertNonNull(values, name);

            if (values.Length == 0)
            {
                throw new ArgumentException(name);
            }
        }
    }
}