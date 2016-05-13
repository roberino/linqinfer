using System;

namespace LinqInfer.Learning.Classification
{
    public class ClassifyResult<T> : IEquatable<ClassifyResult<T>>, IComparable<ClassifyResult<T>>
    {
        public T ClassType { get; set; }

        public double Score { get; set; }

        public int CompareTo(ClassifyResult<T> other)
        {
            if (other == null) return -1;

            return Score.CompareTo(other.Score);
        }

        public bool Equals(ClassifyResult<T> other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            return ClassType.Equals(other.ClassType) && Score == other.Score;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClassifyResult<T>);
        }

        public override int GetHashCode()
        {
            if (ClassType == null) return 0;

            return ClassType.GetHashCode() * 7 * Score.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1:#.##})", ClassType, Score);
        }
    }
}
