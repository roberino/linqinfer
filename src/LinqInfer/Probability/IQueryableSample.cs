using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    public interface IQueryableSample<T> : IQueryable<T>
    {
        string Name { get; }
        int Total();
        IHypotheticalSubset<T> CreateHypothesis(Expression<Func<T, bool>> eventPredicate, string name = null, Fraction? prior = null);
        IQueryableSample<T> Subset(Expression<Func<T, bool>> eventPredicate);
        Tuple<IQueryableSample<T>, IQueryableSample<T>> Split(Expression<Func<T, bool>> eventPredicate);
        Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate);
        Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
        bool IsSimple(Expression<Func<T, bool>> eventPredicate);
        bool IsExhaustive(Expression<Func<T, bool>> eventPredicate);
        bool AreMutuallyExclusive(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
    }
}
