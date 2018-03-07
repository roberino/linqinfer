using LinqInfer.Maths;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class OneHotEncoding<T>
    {
        public OneHotEncoding(ISet<T> classes)
        {
            int i = 0;
            Lookup = classes.ToDictionary(c => c, _ => i++);
        }

        public int VectorSize => Lookup.Count;

        public OneOfNVector Encode(T obj)
        {
            if (Lookup.TryGetValue(obj, out int index))
            {
                return new OneOfNVector(VectorSize, index);
            }

            return new OneOfNVector(VectorSize);
        }

        public BitVector Encode(IEnumerable<T> categories)
        {
            var indexes = new List<int>();

            foreach (var cat in categories)
            {
                if (Lookup.TryGetValue(cat, out int index))
                {
                    indexes.Add(index);
                }
            }

            return new BitVector(indexes, VectorSize);
        }

        public IVector Encode(T[] categories)
        {
            if (categories.Length == 0) return new ZeroVector(VectorSize);
            if (categories.Length == 1) return Encode(categories[0]);
            return Encode(categories as IEnumerable<T>);
        }

        internal IDictionary<T, int> Lookup { get; }
    }
}