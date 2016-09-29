using System;
using System.Diagnostics.Contracts;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a numeric range
    /// </summary>
    public struct Range : IEquatable<Range>, IComparable<Range>
    {
        public Range(double max = 1, double min = 0)
        {
            Contract.Requires(max > min);

            Min = min;
            Max = max;
        }

        /// <summary>
        /// The minimum (inclusive) value
        /// </summary>
        public double Min { get; private set; }

        /// <summary>
        /// The maximum (inclusive) value
        /// </summary>
        public double Max { get; private set; }

        /// <summary>
        /// The size of the range
        /// </summary>
        public double Size { get { return Max - Min; } }

        /// <summary>
        /// Returns true if a value falls within the bounds of the range (inclusive of min and max values)
        /// </summary>
        /// <param name="value">The value</param>
        public bool IsWithin(double value)
        {
            return value >= Min && value <= Max;
        }
        public override string ToString()
        {
            return string.Format("{0:0.00} - {1:0.00}", Min, Max);
        }

        public override bool Equals(object obj)
        {
            try
            {
                return Equals((Range)obj);
            }
            catch
            {
                return false;
            }
        }

        public bool Equals(Range other)
        {
            return Min == other.Min && Max == other.Max;
        }

        public int CompareTo(Range other)
        {
            if (Min == other.Min && Max == other.Max) return 0;

            var mc = Min.CompareTo(other.Min);

            if (mc != 0) return mc;

            return Max.CompareTo(other.Max);
        }

        public override int GetHashCode()
        {
            return (Min * -7 + Max).GetHashCode();
        }
    }
}