using System;
using System.Linq.Expressions;

namespace LinqInfer.Math
{
    public interface IHypotheticalSubset<T> : IHypothetical
    {
        IHypotheticalSubset<T> Update(Expression<Func<T, bool>> newEvidence);
    }
}
