using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Maths.Probability
{
    public interface IQueryableSample<T> : IQueryable<T>
    {
        string Name { get; }
        int Total();
        IHypotheticalSubset<T> CreateHypothesis(Expression<Func<T, bool>> eventPredicate, string name = null, Fraction? prior = null);
        IQueryableSample<T> Subset(Expression<Func<T, bool>> eventPredicate);
        Tuple<IQueryableSample<T>, IQueryableSample<T>> Split(Expression<Func<T, bool>> eventPredicate);
        Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate);
        Fraction ProbabilityOfEventAandB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
        Fraction ProbabilityOfEventAorB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
        Fraction ConditionalProbabilityOfEventAGivenB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
        Fraction PosterierProbabilityOfEventBGivenA(Expression<Func<T, bool>> eventPredicateB, Expression<Func<T, bool>> eventPredicateA);
        Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
        bool IsSimple(Expression<Func<T, bool>> eventPredicate);
        bool IsExhaustive(Expression<Func<T, bool>> eventPredicate);
        bool AreMutuallyExclusive(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);
    }
}
