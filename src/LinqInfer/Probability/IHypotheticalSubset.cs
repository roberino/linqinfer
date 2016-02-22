using System;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    public interface IHypotheticalSubset<T> : IHypothetical
    {
        IHypotheticalSubset<T> Update(Expression<Func<T, bool>> newEvidence);
    }
}
