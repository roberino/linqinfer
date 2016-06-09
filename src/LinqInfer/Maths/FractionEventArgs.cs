using System;

namespace LinqInfer.Maths
{
    public class FractionEventArgs : EventArgs
    {
        public FractionEventArgs(Fraction value)
        {
            Value = value;
        }

        public Fraction Value { get; private set; }
    }
}