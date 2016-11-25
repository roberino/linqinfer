using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths.Probability
{
    internal class DiscreteMarkovChain<T> : IDiscreteMarkovChain<T> where T : IEquatable<T>
    {
        private readonly Transition _root;
        private readonly Random _random;
        private readonly byte _order;

        public DiscreteMarkovChain(byte order = 1)
        {
            Contract.Assert(order > 0);

            _order = order;
            _root = new Transition(default(T));
            _random = new Random();
        }

        public int Order { get { return _order; } }

        public void AnalyseSequences<S>(IEnumerable<S> sequences) where S : IEnumerable<T>
        {
            foreach (var seq in sequences) AnalyseSequence(seq);
        }

        public void AnalyseSequences(IEnumerable<T> sequence, Func<T, bool> delimiter)
        {
            foreach (var seq in sequence.Delimit(delimiter))
            {
                AnalyseSequence(seq);
            }
        }

        public void AnalyseSequence(IEnumerable<T> sequence)
        {
            var history = new Queue<RuntimeTransition>();
            var last = default(T);

            foreach (var state in sequence)
            {
                foreach (var x in history)
                {
                    x.SetFollowing(state);
                }

                if (history.Count == _order)
                {
                    history.Dequeue();
                }

                history.Enqueue(new RuntimeTransition(_root.SetFollowing(state)));

                last = state;
            }
        }

        public IEnumerable<T> Simulate(int maxIterations = 100)
        {
            return Simulate(Functions.FrequencyWeightedRandomSelector(_root.Following.ToDictionary(f => f.Key, f => f.Value.Frequency))(), maxIterations);
        }

        public IEnumerable<T> Simulate(T seedState, int maxIterations = 100)
        {
            var history = new Queue<T>(_order);

            NullableState next = new NullableState() { Value = seedState, HasValue = true };

            history.Enqueue(next.Value);

            for (int i = 0; i < maxIterations; i++)
            {
                yield return next.Value;

                next = SimulateNextInternal(history);

                if (!next.HasValue) break;

                if (history.Count == _order)
                {
                    history.Dequeue();
                }

                history.Enqueue(next.Value);
            }
        }

        public T SimulateNext(T eventValue)
        {
            var freq = GetFrequencies(eventValue);

            if (freq.Count == 0) return default(T);

            return Functions.FrequencyWeightedRandomSelector(freq)();
        }

        public T SimulateNext(IEnumerable<T> transitionStates)
        {
            return SimulateNextInternal(transitionStates).Value;
        }

        public Fraction ProbabilityOfEvent(IEnumerable<T> transitionStates, T nextState)
        {
            var freq = GetFrequencies(transitionStates);

            int f = 0;

            if (freq.TryGetValue(nextState, out f))
            {
                return new Fraction(f, freq.Sum(x => x.Value));
            }

            return Fraction.Zero;
        }

        public IDictionary<T, int> GetFrequencies(T currentState)
        {
            if (currentState != null)
            {
                Transition node;

                if (_root.Following.TryGetValue(currentState, out node))
                {
                    return node.Following.ToDictionary(x => x.Key, x => x.Value.Frequency);
                }
            }

            return new Dictionary<T, int>();
        }

        public IDictionary<T, int> GetFrequencies(IEnumerable<T> transitionStates)
        {
            var hist = transitionStates.Reverse().Take(_order).Reverse().ToList();

            bool pathFound = true;
            Transition node = _root;

            foreach (var w in hist)
            {
                if (!node.Following.TryGetValue(w, out node))
                {
                    pathFound = false;
                    break;
                }
            }

            if (pathFound)
                return node.Following.ToDictionary(x => x.Key, x => x.Value.Frequency);

            return new Dictionary<T, int>();
        }

        private NullableState SimulateNextInternal(IEnumerable<T> transitionStates)
        {
            var freq = GetFrequencies(transitionStates);

            if (freq.Count == 0) return new NullableState { HasValue = false };

            var value = Functions.FrequencyWeightedRandomSelector(freq)();

            return new NullableState { Value = value, HasValue = value != null };
        }

        private struct NullableState
        {
            public bool HasValue { get; set; }
            public T Value { get; set; }
        }

        private class RuntimeTransition
        {
            private Transition _link;

            public RuntimeTransition(Transition link)
            {
                _link = link;
            }

            public void SetFollowing(T state)
            {
                _link = _link.SetFollowing(state);
            }
        }

        private class Transition
        {
            public Transition(T state)
            {
                State = state;
                Following = new Dictionary<T, Transition>(EqualityComparer<T>.Default);
            }

            public T State { get; private set; }

            public int Frequency { get; set; }

            public Transition SetFollowing(T state)
            {
                Transition n;

                if (!Following.TryGetValue(state, out n))
                {
                    Following[state] = n = new Transition(state);
                }

                n.Frequency++;

                return n;
            }

            public IDictionary<T, Transition> Following { get; private set; }
        }
    }
}