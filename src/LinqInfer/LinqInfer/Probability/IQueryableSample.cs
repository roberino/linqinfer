using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    public interface IQueryableSample<T> : IQueryable<T>
    {
        Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate);
        Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
        bool IsSimple(Expression<Func<T, bool>> eventPredicate);
        bool IsExhaustive(Expression<Func<T, bool>> eventPredicate);
        bool AreMutuallyExclusive(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
    }
}
