using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Genetics
{
    internal static class GeneticExtensions
    {
        public static void EvolveOverIterations<P, O>(this IEnumerable<P> initialParameters, Func<P, O> factory, Func<O, double> fitnessFunction, Func<O, int, bool> haltingFunction = null) where P : ICr<P>
        {
            int i = 0;

            var breedingSet = initialParameters.Select(p => new Item<P, O> { Parameters = p, Instance = factory(p), Score = 0 }).ToList();
            var fittest = breedingSet.Select(x => x.Instance).OrderByDescending(fitnessFunction).FirstOrDefault();

            while (!haltingFunction(fittest, i))
            {
                breedingSet.AsParallel().ForAll(x =>
                {
                    x.Score = fitnessFunction(x.Instance);
                });

                breedingSet = breedingSet.OrderByDescending(x => x.Score).Take(Math.Max(breedingSet.Count - 4, 2)).ToList();

                var newParam1 = breedingSet[0].Parameters.Breed(breedingSet[1].Parameters);

                breedingSet.Add(new Item<P, O>() { Parameters = newParam1, Instance = factory(newParam1) });

                if (breedingSet.Count > 4)
                {
                    var newParam2 = breedingSet[2].Parameters.Breed(breedingSet[3].Parameters);

                    breedingSet.Add(new Item<P, O>() { Parameters = newParam1, Instance = factory(newParam1) });
                }

                i++;
            }
        }

        private class Item<P, O>
        {
            public P Parameters;
            public O Instance;
            public double Score;
        }
    }
}
