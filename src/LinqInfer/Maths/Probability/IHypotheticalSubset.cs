using System;
using System.Linq.Expressions;

namespace LinqInfer.Maths.Probability
{
    public interface IHypotheticalSubset<T> : IHypothetical
    {
        IHypotheticalSubset<T> Update(Expression<Func<T, bool>> newEvidence);
    }
}
