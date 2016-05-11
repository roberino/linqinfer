using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    class CategoricalClassifier<T> : IClassifier<T, bool>, IAssistedLearning<T, bool>
    {
        private readonly IDictionary<T, ClassData> _histogram;

        public CategoricalClassifier()
        {
            _histogram = new Dictionary<T, ClassData>();
        }

        public ClassifyResult<T> Classify(bool[] vector)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ClassifyResult<T>> FindPossibleMatches(bool[] vector)
        {
            throw new NotImplementedException();
        }

        public double Train(T item, bool[] sample)
        {
            ClassData data;

            if(!_histogram.TryGetValue(item, out data))
            {
                _histogram[item] = data = new ClassData();
            }

            int i = 0;

            foreach(var x in sample)
            {
                data.CategoricalFrequencies[i]++;
            }

            data.ClassFrequency += 1;

            return 0;
        }

        private class ClassData
        {
            public ClassData()
            {
                CategoricalFrequencies = new Dictionary<int, int>();
            }

            public int ClassFrequency { get; set; }

            public IDictionary<int, int> CategoricalFrequencies { get; set; }
        }
    }
}
