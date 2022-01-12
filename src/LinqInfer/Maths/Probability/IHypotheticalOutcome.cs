using System;

namespace LinqInfer.Maths.Probability
{
    public interface IHypotheticalOutcome<T> : IHypothetical, IEquatable<IHypotheticalOutcome<T>>
    {
        T Outcome { get; }
        Fraction Calculate(Fraction likelihoodGivenHypo, Fraction likelihood);
        IHypotheticalOutcome<T> Update(Fraction likelihoodGivenHypo, Fraction likelihood);
        event Action<Fraction> Updated;
    }
}
