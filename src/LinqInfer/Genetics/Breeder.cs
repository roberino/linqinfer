using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Genetics
{
    internal class Breeder<T>
    {
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;

        public T Breed(T parent1, T parent2)
        {
            return default(T);
        }
    }
}
