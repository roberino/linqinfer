using System.Linq;

namespace LinqInfer.Probability
{
    public static class ProbabilityExtensions
    {
        public static IQueryableSample<T> AsSampleSpace<T>(this IQueryable<T> sampleSpace)
        {
            return new QueryableSample<T>(sampleSpace);
        }
    }
}
