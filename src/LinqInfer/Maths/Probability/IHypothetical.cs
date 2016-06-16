using System;

namespace LinqInfer.Maths.Probability
{
    /// <summary>
    /// Represents something that is hypothetical
    /// </summary>
    public interface IHypothetical
    {
        /// <summary>
        /// The name of the thing
        /// </summary>
        string Name { get; }
        /// <summary>
        /// The prior probability of the hypothesis
        /// </summary>
        Fraction PriorProbability { get; }
        /// <summary>
        /// The Posterior probability of the hypothesis
        /// </summary>
        Fraction PosteriorProbability { get; }
    }
}
