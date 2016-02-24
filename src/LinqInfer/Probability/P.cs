using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Probability
{
    public static class P
    {
        public static HypothesisBuilder<T> Of<T>(T eventT)
        {
            return new HypothesisBuilder<T>(eventT);
        }

        public static Fraction Sum(this IEnumerable<Fraction> values)
        {
            Fraction total = values.First();

            foreach (var v in values.Skip(1))
            {
                total += v;
            }

            return total;
        }

        public static Hypothetheses<T> AsHypotheses<T>(this IEnumerable<IHypotheticalOutcome<T>> hypos)
        {
            return new Hypothetheses<T>(hypos);
        }

        public static Hypothetheses<T> Hypotheses<T>(params IHypotheticalOutcome<T>[] hypos)
        {
            return new Hypothetheses<T>(hypos);
        }

        public static Hypothetheses<int> Hypotheses(params Fraction[] hypos)
        {
            int n = 0;
            return new Hypothetheses<int>(hypos.Select(h => new HypotheticalOutcome<int>(n++, h)).ToList());
        }

        public static IHypotheticalOutcome<T> Hypothesis<T>(T pEvent, Fraction priorProbability)
        {
            return new HypotheticalOutcome<T>(pEvent, priorProbability);
        }

        public static Fraction OutOf(this int n, int d)
        {
            return new Fraction(n, d);
        }

        public static Hypothetheses<T> AsHypotheses<T>(this IDictionary<T, int> sampleSpace)
        {
            var total = sampleSpace.Sum(h => h.Value);
            var hypos = sampleSpace.Select(h => new HypotheticalOutcome<T>(h.Key, new Fraction(h.Value, total)));

            return Hypotheses(hypos.ToArray());

            //return new QueryableSample<KeyValuePair<T, int>>(sampleSpace.AsQueryable(), null, x => x.Value);
        }

        public static IQueryableSample<T> AsSampleSpace<T>(this IQueryable<T> sampleSpace)
        {
            return new QueryableSample<T>(sampleSpace);
        }
    }
}
