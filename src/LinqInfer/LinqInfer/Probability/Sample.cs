using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    public class Sample<T> : SampleBase<T>
    {
        protected readonly ISet<T> _sampleSpace;

        public Sample(ISet<T> sampleSpace, string name = null)
        {
            _sampleSpace = sampleSpace;
            Name = name;
            Logger = (m) => Console.WriteLine(m);
        }

        public Sample(IEnumerable<T> sampleSpace, string name = null)
        {
            _sampleSpace = new HashSet<T>(sampleSpace);
            Name = name;
            Logger = (m) => Console.WriteLine(m);
        }

        public Sample(string name = null)
        {
            _sampleSpace = new HashSet<T>();
            Name = name;
            Logger = (m) => Console.WriteLine(m);
        }

        public void Add(params T[] items)
        {
            foreach(var r in items)
            {
                _sampleSpace.Add(r);
            }
        }
        public void Add(int count, Func<int, T> generator)
        {
            foreach (var r in Enumerable.Range(0, count).Select(n => generator(n)))
            {
                _sampleSpace.Add(r);
            }
        }

        public Sample<T> Subset(Expression<Func<T, bool>> eventPredicate)
        {
            return new Sample<T>(_sampleSpace.Where(eventPredicate.Compile()));
        }

        public override int Count()
        {
            return _sampleSpace.Count;
        }

        public override int Count(Expression<Func<T, bool>> eventPredicate)
        {
            return _sampleSpace.Count(eventPredicate.Compile());
        }

        public override Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate)
        {
            var e = _sampleSpace.Where(eventPredicate.Compile()).Count();
            var n = _sampleSpace.Count;

            return Output(new Fraction(e, n));
        }

        public override Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            var a = _sampleSpace.Where(eventPredicateA.Compile());
            var b_a = a.Where(eventPredicateB.Compile());

            return new Fraction(b_a.Count(), a.Count());
        }

        public bool IsSimple(Expression<Func<T, bool>> eventPredicate)
        {
            return _sampleSpace.Count(eventPredicate.Compile()) == 1;
        }

        public bool IsExhaustive(Expression<Func<T, bool>> eventPredicate)
        {
            return _sampleSpace.Any(e => !eventPredicate.Compile()(e));
        }

        public bool AreMutuallyExclusive(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            return Output(!_sampleSpace.Any(e => eventPredicateA.Compile()(e) && eventPredicateB.Compile()(e)));
        }
    }
}
