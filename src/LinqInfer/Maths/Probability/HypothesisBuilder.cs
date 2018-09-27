namespace LinqInfer.Maths.Probability
{
    public sealed class HypothesisBuilder<T>
    {
        readonly T _outcome;

        internal HypothesisBuilder(T outcome)
        {
            _outcome = outcome;
        }

        public FractionBuilder Is(int numerator)
        {
            return new FractionBuilder(this, numerator);
        }

        public IHypotheticalOutcome<T> Is(Fraction prior)
        {
            return new HypotheticalOutcome<T>(_outcome, prior);
        }

        public sealed class FractionBuilder
        {
            readonly HypothesisBuilder<T> _owner;
            readonly int _numerator;

            internal FractionBuilder(HypothesisBuilder<T> owner, int numerator)
            {
                _owner = owner;
                _numerator = numerator;
            }
            
            public IHypotheticalOutcome<T> OutOf(int denominator)
            {
                return _owner.Is(new Fraction(_numerator, denominator));
            }
        }
    }
}
