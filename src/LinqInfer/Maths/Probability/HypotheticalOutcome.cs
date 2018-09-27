using System;

namespace LinqInfer.Maths.Probability
{
    class HypotheticalOutcome<T> : IHypotheticalOutcome<T>
    {
        public HypotheticalOutcome(T outcome, Fraction prior, string name = null)
        {
            Outcome = outcome;
            Name = outcome.ToString();
            PriorProbability = prior;
            PosteriorProbability = prior;
        }

        public event EventHandler<FractionEventArgs> Updated;

        public T Outcome { get; }

        public string Name { get; }

        public Fraction PriorProbability { get; }

        public Fraction PosteriorProbability { get; private set; }

        public Fraction Calculate(Fraction likelyhoodGivenHypo, Fraction likelyhood)
        {
            return Fraction.Divide(Fraction.Multiply(PosteriorProbability, likelyhoodGivenHypo, true), likelyhood, true);
        }

        public IHypotheticalOutcome<T> Update(Fraction likelyhoodGivenHypo, Fraction likelyhood)
        {
            PosteriorProbability = Calculate(likelyhoodGivenHypo, likelyhood);

            var ev = Updated;

            if (ev != null)
            {
                ev.Invoke(this, new FractionEventArgs(PosteriorProbability));
            }

            return this;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Outcome.GetHashCode() * 7 * PosteriorProbability.GetHashCode();
        }

        public bool Equals(IHypotheticalOutcome<T> other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            return other.PosteriorProbability == PosteriorProbability && Outcome.Equals(other.Outcome);
        }
    }
}
