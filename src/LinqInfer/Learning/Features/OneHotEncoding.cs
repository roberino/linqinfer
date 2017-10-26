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

        internal IDictionary<T, int> Lookup { get; }
    }
}