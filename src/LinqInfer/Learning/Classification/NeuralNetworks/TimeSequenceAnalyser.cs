using System;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class TimeSequenceAnalyser<TInput> : ITimeSequenceAnalyser<TInput> where TInput : IEquatable<TInput>
    {
        readonly INetworkClassifier<TInput, TInput> _networkClassifier;

        bool hasInput;
        TInput lastInput;

        public TimeSequenceAnalyser(INetworkClassifier<TInput, TInput> networkClassifier)
        {
            _networkClassifier = networkClassifier;
        }

        public void Reset()
        {
            _networkClassifier.Reset();
            lastInput = default;
            hasInput = false;
        }

        public IEnumerable<TInput> Simulate(int maxIterations = 100)
        {
            yield break;
        }

        public IEnumerable<TInput> Simulate(TInput seedState, int maxIterations = 100)
        {
            var next = SimulateNext(seedState);

            if (next.Equals(default))
            {
                yield break;
            }

            foreach (var n in Enumerable.Range(1, maxIterations))
            {
                next = SimulateNext(next);

                if (next.Equals(default))
                {
                    break;
                }

                yield return next;
            }
        }

        public TInput SimulateNext(TInput currentState)
        {
            var result = _networkClassifier.Classify(currentState).FirstOrDefault();

            if (result != null)
            {
                return result.ClassType;
            }

            return default;
        }

        public TInput SimulateNext(IEnumerable<TInput> transitionStates)
        {
            ClassifyResult<TInput> lastResult = null;

            foreach (var state in transitionStates)
            {
                var results = _networkClassifier.Classify(state).ToArray();
                lastResult = results.FirstOrDefault();
            }

            if (lastResult != null)
            {
                return lastResult.ClassType;
            }

            return default;
        }

        public void Train(IEnumerable<TInput> sequence)
        {
            foreach (var item in sequence)
            {
                Train(item);
            }
        }

        public void Train(TInput input)
        {
            if (hasInput)
            {
                _networkClassifier.Train(lastInput, input);
            }

            lastInput = input;
            hasInput = true;
        }

        public PortableDataDocument ExportData()
        {
            var baseDoc = _networkClassifier.ExportData();

            baseDoc.SetType(this);

            return baseDoc;
        }

        public static ITimeSequenceAnalyser<TInput> Create(PortableDataDocument data)
        {
            var classifier = MultilayerNetworkObjectClassifier<TInput, TInput>.Create(data);

            return new TimeSequenceAnalyser<TInput>(classifier);
        }
    }
}