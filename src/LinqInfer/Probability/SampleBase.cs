using System;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    public abstract class SampleBase<T>
    {
        public Action<string> Logger { get; set; }

        public string Name { get; protected set; }

        public abstract int Count();

        public abstract int Count(Expression<Func<T, bool>> eventPredicate);

        public abstract Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate);

        public Fraction ProbabilityOfEventAandB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            var pA = ProbabilityOfEvent(eventPredicateA);
            var pB = ProbabilityOfEvent(eventPredicateB);

            return pA * pB;
        }

        public virtual Fraction ProbabilityOfEventAorB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            var pA = ProbabilityOfEvent(eventPredicateA);
            var pB = ProbabilityOfEvent(eventPredicateB);
            var pAB = ProbabilityOfEvent(x => eventPredicateA.Compile()(x) && eventPredicateB.Compile()(x));

            return pA + pB - pAB;
        }

        public Fraction ConditionalProbabilityOfEventAGivenB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB)
        {
            // P(A|B) = P(A and B) / P(B)
            // e.g. Jack (J) in deck of cards = 4/52
            //      Red card (R) = 26/52
            //      Jack given it is a red card 
            //      (J|R) = P(J) * P(R) / P(R) ?? 
            //      = 2/26 = 1/13

            return ProbabilityOfEventAandB(eventPredicateA, eventPredicateB) / ProbabilityOfEvent(eventPredicateB);
        }

        public abstract Fraction LikelyhoodOfB(Expression<Func<T, bool>> eventPredicateA, Expression<Func<T, bool>> eventPredicateB);

        public Fraction PosterierProbabilityOfEventBGivenA(Expression<Func<T, bool>> eventPredicateB, Expression<Func<T, bool>> eventPredicateA)
        {
            // P(B|A) = P(A)P(B|A) / P(B)

            return (ProbabilityOfEvent(eventPredicateA) * LikelyhoodOfB(eventPredicateA, eventPredicateB)) / ProbabilityOfEvent(eventPredicateA);
        }

        protected T1 Output<T1>(T1 value)
        {
            if (Logger != null)
            {
                var frame = new System.Diagnostics.StackFrame(1);

                Output("{0} {1} = {2}", Name ?? GetType().Name, frame.GetMethod().Name, value);
            }

            return value;
        }

        protected void Output(string msg, params object[] args)
        {
            var o = Logger;

            if (o != null)
            {
                o.Invoke(string.Format(msg, args));
            }
        }
    }
}
