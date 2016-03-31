using LinqInfer.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Math
{
    internal class QueryableSample<T> : SampleBase<T>, IQueryableSample<T>
    {
        protected readonly IQueryable<T> _sampleSpace;
        protected readonly Expression<Func<T, int>> _weightSelector;
        private int _hypothesisCount = 0;
        protected bool _hasWeights;

        public QueryableSample(IQueryable<T> sampleSpace, string name = null, Expression<Func<T, int>> weightSelector = null)
        {
            _sampleSpace = sampleSpace;
            _hasWeights = weightSelector != null;
            _weightSelector = weightSelector ?? (x => 1);
            Name = name;
            Logger = (m) => Console.WriteLine(m);
        }

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

        public IHypotheticalSubset<T> CreateHypothesis(Expression<Func<T, bool>> eventPredicate, string name = null, Fraction? prior = null)
        {
            _hypothesisCount++;
            return new HypothesisSubset<T>(this, Subset(eventPredicate), name ?? ("Hypothesis " + _hypothesisCount), prior, _hasWeights ? _weightSelector : null);
        }

        public IQueryableSample<T> Subset(Expression<Func<T, bool>> eventPredicate)
        {
            return new QueryableSample<T>(_sampleSpace.Where(eventPredicate), null, _hasWeights ? _weightSelector : null);
        }

        public Tuple<IQueryableSample<T>, IQueryableSample<T>> Split(Expression<Func<T, bool>> eventPredicate)
        {
            return new Tuple<IQueryableSample<T>, IQueryableSample<T>>(Subset(eventPredicate), Subset(eventPredicate.Invert()));
        }

        private int WeightedSum(IQueryable<T> sample)
        {
            return _hasWeights ? sample.Select(_weightSelector).Sum() : sample.Count();
        }

        public override int Total()
        {
            return WeightedSum(_sampleSpace);
        }

        public override int Count(Expression<Func<T, bool>> eventPredicate)
        {
            return WeightedSum(_sampleSpace.Where(eventPredicate));
        }

        public override Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate)
        {
            var e = Count(eventPredicate);
            var n = Total();

            return Output(new Fraction(e, n));
        }

        public override Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            var a = _sampleSpace.Where(eventPredicateA);
            var b_a = a.Where(eventPredicateB);

            return new Fraction(WeightedSum(b_a), WeightedSum(a));
        }

        public bool IsSimple(Expression<Func<T, bool>> eventPredicate)
        {
            return Count(eventPredicate) == 1;
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
