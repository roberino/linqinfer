using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    public class WeightedSample<T> : SampleBase<T>
    {
        private readonly IDictionary<T, int> data;

        public WeightedSample(string name = null) : this(new Dictionary<T, int>(32), name)
        {
        }

        public WeightedSample(IDictionary<T, int> data, string name = null)
        {
            this.data = data;
            Name = name;
        }

        public int this[T item]
        {
            get
            {
                return data[item];
            }
            set
            {
                data[item] = value;
            }
        }

        public void Add(T item, int weight)
        {
            if (data.ContainsKey(item))
            {
                data[item] += weight;
            }
            else
            {
                data[item] = weight;
            }
        }

        public override int Total()
        {
            return Output(data.Sum(x => x.Value));
        }

        public override int Count(Expression<Func<T, bool>> eventPredicate)
        {
            return Output(data.Where(x => eventPredicate.Compile()(x.Key)).Sum(x => x.Value));
        }

        public override Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate)
        {
            return Output(new Fraction(data.Where(x => eventPredicate.Compile()(x.Key)).Sum(x => x.Value), Total()));
        }

        public override Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            var a = data.Where(x => eventPredicateA.Compile()(x.Key));
            var b_a = a.Where(x => eventPredicateB.Compile()(x.Key));

            return Output(new Fraction(b_a.Sum(x => x.Value), a.Sum(x => x.Value)));
        }
    }
}
