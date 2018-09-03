namespace LinqInfer.Utility.Expressions
{
    public sealed class Token
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
    }
}
