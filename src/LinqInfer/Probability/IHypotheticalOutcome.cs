namespace LinqInfer.Probability
{
    public interface IHypotheticalOutcome<T> : IHypothetical
    {
        T Outcome { get; }
        IHypotheticalOutcome<T> Update(Fraction likelyhoodGivenHypo, Fraction likelyhood);
    }
}
