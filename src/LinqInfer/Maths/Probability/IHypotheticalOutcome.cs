namespace LinqInfer.Maths.Probability
{
    public interface IHypotheticalOutcome<T> : IHypothetical
    {
        T Outcome { get; }
        Fraction Calculate(Fraction likelyhoodGivenHypo, Fraction likelyhood);
        IHypotheticalOutcome<T> Update(Fraction likelyhoodGivenHypo, Fraction likelyhood);
    }
}
