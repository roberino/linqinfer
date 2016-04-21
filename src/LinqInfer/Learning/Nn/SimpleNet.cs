using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    internal class SimpleNet<T> : IByteClassifier<T>, IFloatingPointClassifier<T>, IAssistedLearning<T, byte>, IAssistedLearning<T, float>
    {
        private readonly Dictionary<T, List<InputAggregator>> netData;
        private readonly int vectorSize;
        private readonly Func<InputAggregator> neuronGenerator;

        public SimpleNet(int vectorSize, Func<InputAggregator> neuronGenerator = null)
        {
            this.vectorSize = vectorSize;
            this.neuronGenerator = neuronGenerator ?? (() => new InputAggregator());
            netData = new Dictionary<T, List<InputAggregator>>();
        }

        public ClassifyResult<T> Classify(byte[] data)
        {
            return FindPossibleMatches(data).FirstOrDefault();
        }

        public ClassifyResult<T> Classify(float[] data)
        {
            return FindPossibleMatches(data).FirstOrDefault();
        }

        public double Train(T dataClass, byte[] sample)
        {
            Contract.Assert(sample != null);

            return Train(dataClass, sample.Select(v => (float)v).ToArray());
        }

        public double Train(T dataClass, float[] sample)
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

        public IEnumerable<ClassifyResult<T>> FindPossibleMatches(float[] data)
        {
            Contract.Assert(data != null);
            Contract.Assert(data.Length == vectorSize);

            if (netData.Count == 0)
            {
                throw new InvalidOperationException("No training data");
            }

            var results = new ConcurrentDictionary<T, double>();

            netData.AsParallel().ForAll(s =>
            {
                int i = 0;
                double t = 0;

                foreach (var x in data)
                {
                    var n = s.Value[i++];

                    t += n.Pdf(x);
                }

                results[s.Key] = t;
            });

            return results
                .OrderByDescending(r => r.Value)
                .Select(r => new ClassifyResult<T>() { ClassType = r.Key, Score = r.Value });
        }

        public IEnumerable<ClassifyResult<T>> FindPossibleMatches(byte[] vector)
        {
            Contract.Assert(vector != null);

            return FindPossibleMatches(vector.Select(v => (float)v).ToArray());
        }
    }
}