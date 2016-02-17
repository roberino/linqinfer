using LinqInfer.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    internal class QueryableSample<T> : SampleBase<T>, IQueryableSample<T>
    {
        protected readonly IQueryable<T> _sampleSpace;

        public Expression Expression
        {
            get
            {
                return _sampleSpace.Expression;
            }
        }

        public Type ElementType
        {
            get
            {
                return _sampleSpace.ElementType;
            }
        }

        public IQueryProvider Provider
        {
            get
            {
                return _sampleSpace.Provider;
            }
        }

        public QueryableSample(IQueryable<T> sampleSpace, string name = null)
        {
            _sampleSpace = sampleSpace;
            Name = name;
            Logger = (m) => Console.WriteLine(m);
        }

        public IQueryableSample<T> Subset(Expression<Func<T, bool>> eventPredicate)
        {
            return new QueryableSample<T>(_sampleSpace.Where(eventPredicate));
        }

        public override int Count()
        {
            return _sampleSpace.Count();
        }

        public override int Count(Expression<Func<T, bool>> eventPredicate)
        {
            return _sampleSpace.Count(eventPredicate);
        }

        public override Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate)
        {
            var e = _sampleSpace.Where(eventPredicate).Count();
            var n = _sampleSpace.Count();

            return Output(new Fraction(e, n));
        }

        public override Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            var a = _sampleSpace.Where(eventPredicateA);
            var b_a = a.Where(eventPredicateB);

            return new Fraction(b_a.Count(), a.Count());
        }

        public bool IsSimple(Expression<Func<T, bool>> eventPredicate)
        {
            return _sampleSpace.Count(eventPredicate) == 1;
        }

        public bool IsExhaustive(Expression<Func<T, bool>> eventPredicate)
        {
            return _sampleSpace.All(eventPredicate);
        }

        public bool AreMutuallyExclusive(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            var conjExp = eventPredicateA.ConjunctiveJoin(eventPredicateB);
            return Output(!_sampleSpace.Any(conjExp));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _sampleSpace.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _sampleSpace.GetEnumerator();
        }
    }
}
