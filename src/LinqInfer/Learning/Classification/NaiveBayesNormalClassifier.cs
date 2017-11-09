using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal class NaiveBayesNormalClassifier<T> : 
        IByteClassifier<T>, 
        IFloatingPointClassifier<T>, 
        IAssistedLearning<T, byte>, 
        IAssistedLearning<T, double>
    {
        private readonly Dictionary<T, List<InputAggregator>> netData;
        private readonly int vectorSize;
        private readonly Func<InputAggregator> neuronGenerator;

        public NaiveBayesNormalClassifier(int vectorSize, Func<InputAggregator> neuronGenerator = null)
        {
            this.vectorSize = vectorSize;
            this.neuronGenerator = neuronGenerator ?? (() => new InputAggregator());
            netData = new Dictionary<T, List<InputAggregator>>();
        }

        public ClassifyResult<T> ClassifyAsBestMatch(byte[] data)
        {
            return Classify(data).FirstOrDefault();
        }

        public ClassifyResult<T> ClassifyAsBestMatch(double[] data)
        {
            return Classify(data).FirstOrDefault();
        }

        public double Train(T dataClass, byte[] sample)
        {
            Contract.Assert(sample != null);

            return Train(dataClass, sample.Select(v => (double)v).ToArray());
        }

        public double Train(T dataClass, double[] sample)
        {
            Contract.Assert(sample != null);
            Contract.Assert(sample.Length == vectorSize);

            List<InputAggregator> neurons;

            if (!netData.TryGetValue(dataClass, out neurons))
            {
                netData[dataClass] = neurons = Enumerable.Range(0, vectorSize).Select(n => neuronGenerator()).ToList();
            }

            int i = 0;

            foreach (var x in sample)
            {
                neurons[i++].AddSample(x);
            }

            return 0;
        }

        public IEnumerable<ClassifyResult<T>> Classify(double[] data)
        {
            Contract.Assert(data != null);
            Contract.Assert(data.Length == vectorSize);

            if (netData.Count == 0)
            {
                throw new InvalidOperationException("No training data");
            }

            var results = new ConcurrentDictionary<T, double>();
            var total = netData.Sum(n => n.Value.Count);

            netData.AsParallel().ForAll(s =>
            {
                int i = 0;
                double t = (double)s.Value.Count / (double)total;

                foreach (var x in data)
                {
                    var n = s.Value[i++];
                    if (n.Theta != 0)
                    {
                        var p = n.Pdf(x);
                        t *= p;
                    }
                }

                results[s.Key] = t;
            });

            return results
                .OrderByDescending(r => r.Value)
                .Select(r => new ClassifyResult<T>() { ClassType = r.Key, Score = r.Value });
        }

        public IEnumerable<ClassifyResult<T>> Classify(byte[] vector)
        {
            Contract.Assert(vector != null);

            return Classify(vector.Select(v => (double)v).ToArray());
        }
    }
}