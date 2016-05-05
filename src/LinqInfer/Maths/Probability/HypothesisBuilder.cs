namespace LinqInfer.Maths.Probability
{
    public class HypothesisBuilder<T>
    {
        private readonly T _outcome;

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

        public class FractionBuilder
        {
            private readonly HypothesisBuilder<T> _owner;
            private readonly int _numerator;

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
