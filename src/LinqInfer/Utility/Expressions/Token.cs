using System;

namespace LinqInfer.Utility.Expressions
{
    public sealed class Token : IEquatable<Token>
    {
        readonly string _value;

        internal Token(string value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return _value;
        }

        public override int GetHashCode()
        {
            return (_value?.GetHashCode()).GetValueOrDefault();
        }

        public bool Equals(Token other)
        {
            if (other == null) return false;

            if (ReferenceEquals(other, this)) return true;

            return string.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Token);
        }
    }
}