using System;

namespace LinqInfer.Maths.Probability
{
    public interface IHypotheticalOutcome<T> : IHypothetical, IEquatable<IHypotheticalOutcome<T>>
    {
        T Outcome { get; }
        Fraction Calculate(Fraction likelyhoodGivenHypo, Fraction likelyhood);
        IHypotheticalOutcome<T> Update(Fraction likelyhoodGivenHypo, Fraction likelyhood);
        event EventHandler<FractionEventArgs> Updated;
    }
}
