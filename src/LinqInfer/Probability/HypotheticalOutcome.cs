namespace LinqInfer.Probability
{
    internal class HypotheticalOutcome<T> : IHypotheticalOutcome<T>
    {
        public HypotheticalOutcome(T outcome, Fraction prior, string name = null)
        {
            Outcome = outcome;
            Name = outcome.ToString();
            PriorProbability = prior;
            PosteriorProbability = prior;
        }

        public T Outcome { get; private set; }

        public string Name { get; private set; }

        public Fraction PriorProbability { get; private set; }

        public Fraction PosteriorProbability { get; private set; }

        public IHypotheticalOutcome<T> Update(Fraction likelyhoodGivenHypo, Fraction likelyhood)
        {
            PosteriorProbability = (PosteriorProbability * likelyhoodGivenHypo) / likelyhood;

            return this;
        }
    }
}
