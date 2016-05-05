using System;

namespace LinqInfer.Maths
{
    [Serializable]
    public struct Range : IEquatable<Range>, IComparable<Range>
    {
        public Range(double max = 1, double min = 0)
        {
            Min = min;
            Max = max;
        }

        public double Min { get; private set; }
        public double Max { get; private set; }
        public double Size { get { return Max - Min; } }
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