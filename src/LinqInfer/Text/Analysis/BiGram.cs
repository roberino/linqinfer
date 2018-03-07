using System;

namespace LinqInfer.Text.Analysis
{
    public sealed class BiGram : IEquatable<BiGram>
    {
        public BiGram(string input = null)
        {
            Input = input;
        }

        public BiGram(string input, string output) : this(input)
        {
            Output = output;
        }

        public string Input { get; }
        public string Output { get; }

        public bool Equals(BiGram other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Input, other.Input) && string.Equals(Output, other.Output);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BiGram);
        }

        public override string ToString()
        {
            return $"{Input} => {Output}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}