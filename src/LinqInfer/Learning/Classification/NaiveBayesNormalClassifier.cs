using LinqInfer.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    class NaiveBayesNormalClassifier<T> : 
        IFloatingPointClassifier<T>, 
        IAssistedLearningProcessor<T, byte>, 
        IAssistedLearningProcessor<T, double>
    {
        readonly Dictionary<T, List<NaiveInputSampler>> netData;
        readonly int vectorSize;
        readonly Func<NaiveInputSampler> neuronGenerator;

        public NaiveBayesNormalClassifier(int vectorSize, Func<NaiveInputSampler> neuronGenerator = null)
        {
            this.vectorSize = vectorSize;
            this.neuronGenerator = neuronGenerator ?? (() => new NaiveInputSampler());
            netData = new Dictionary<T, List<NaiveInputSampler>>();
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
            ArgAssert.AssertNonNull(sample, nameof(sample));
            ArgAssert.AssertEquals(sample.Length, vectorSize, nameof(vectorSize));

            if (!netData.TryGetValue(dataClass, out List<NaiveInputSampler> neurons))
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