using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    internal class Cluster<T> : HashSet<T>
    {
        public Cluster(int level = 0)
        {
            Level = level;
        }

        public Cluster(IEnumerable<T> values, int level)
        {
            foreach (var v in values) Add(v);
            Level = level;
        }

        public IEnumerable<Cluster<T>> Regroup(Cluster<T> other)
        {
            var combine = new Cluster<T>(other.Intersect(this), Level + 1);

            if (combine.Any())
            {
                yield return new Cluster<T>(this.Except(combine), Level + 1);
                yield return new Cluster<T>(other.Except(combine), Level + 1);
                yield return combine;
            }

            yield break;
        }

        public int Level { get; private set; }
    }
}
