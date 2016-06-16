using System;
using System.Linq.Expressions;

namespace LinqInfer.Maths.Probability
{
    public interface IHypotheticalSubset<T> : IHypothetical
    {
        /// <summary>
        /// Updates the set using an expression that represents new evidence
        /// </summary>
        /// <param name="newEvidence"></param>
        /// <returns></returns>
        IHypotheticalSubset<T> Update(Expression<Func<T, bool>> newEvidence);
    }
}
