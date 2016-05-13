using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal class CategoricalClassifier<TClass> : IClassifier<TClass, bool>, IAssistedLearning<TClass, bool>
    {
        private readonly IDictionary<TClass, ClassData> _histogram;
        private readonly int _vectorSize;

        public CategoricalClassifier(int vectorSize)
        {
            _vectorSize = vectorSize;
            _histogram = new Dictionary<TClass, ClassData>();
        }

        public ClassifyResult<TClass> ClassifyAsBestMatch(params bool[] vector)
        {
            return Classify(vector).FirstOrDefault();
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(params bool[] vector)
        {
            return Calculate(vector).OrderByDescending(r => r.Score);
        }

        public double Train(TClass item, params bool[] sample)
        {
            Contract.Assert(sample.Length == _vectorSize);
            
            ClassData data;

            if(!_histogram.TryGetValue(item, out data))
            {
                _histogram[item] = data = new ClassData();
            }

            int i = 0;

            foreach (var x in sample)
            {
                int f;

                data.CategoricalFrequencies.TryGetValue(i, out f);
                
                data.CategoricalFrequencies[i] = x ? f + 1 : f;

                i++;
            }

            data.ClassFrequency += 1;

            return 0;
        }

        private IEnumerable<ClassifyResult<TClass>> Calculate(bool[] vector)
        {
            var total = (double)_histogram.Sum(x => x.Value.ClassFrequency);

            foreach (var cls in _histogram)
            {
                var prior = cls.Value.ClassFrequency / total;

                var likelyhood = vector
                    .Zip(cls.Value.CategoricalFrequencies, (x, t) => ((x ? t.Value : 0) + 1d))
                    .ToList();

                var posterier = likelyhood.Aggregate(prior, (p, x) => x * p);

                yield return new ClassifyResult<TClass>() { Score = posterier, ClassType = cls.Key };
            }
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
