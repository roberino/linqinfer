using System;
using System.Linq.Expressions;

namespace LinqInfer.Maths
{
    public interface IHypotheticalSubset<T> : IHypothetical
    {
        IHypotheticalSubset<T> Update(Expression<Func<T, bool>> newEvidence);
    }
}
