using System;

namespace LinqInfer.Maths.Graphs
{
    public class DataLabel<T> : IEquatable<DataLabel<T>>
    {
        public DataLabel(string label = null)
        {
            Label = label;
        }

        public DataLabel(T data, string label = null)
        {
            Data = data;
            Label = label;
            HasValue = true;
        }

        public string Label { get; }

        public bool HasValue { get; }

        public T Data { get; }

        public bool Equals(DataLabel<T> other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            if ((other.HasValue && !HasValue) || (HasValue && !other.HasValue))
            {
                return false;
            }

            if (!string.Equals(other.Label, Label)) return false;

            if (other.HasValue && HasValue)
            {
                if (other.Data is IEquatable<T>)
                {
                    return ((IEquatable<T>)other.Data).Equals(Data as IEquatable<T>);
                }
                return other.Data.Equals(Data);
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DataLabel<T>);
        }

        public override int GetHashCode()
        {
            return $"{Label}/{(HasValue ? Data.GetHashCode() : 0)}".GetHashCode();
        }
    }
}