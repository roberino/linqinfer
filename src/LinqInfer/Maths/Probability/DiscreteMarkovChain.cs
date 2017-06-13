using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LinqInfer.Data;

namespace LinqInfer.Maths.Probability
{
    internal class DiscreteMarkovChain<T> : IDiscreteMarkovChain<T> where T : IEquatable<T>
    {
        private readonly Func<T, string> _valueExportFunc;
        private readonly Func<string, T> _valueImportFunc;
        private readonly Transition _root;
        private readonly byte _order;

        public DiscreteMarkovChain(byte order = 1, Func<T, string> valueExportFunc = null, Func<string, T> valueImportFunc = null)
        {
            Contract.Assert(order > 0);

            _valueExportFunc = valueExportFunc;
            _valueImportFunc = valueImportFunc;
            _order = order;
            _root = new Transition();
        }

        public DiscreteMarkovChain(BinaryVectorDocument data, Func<T, string> valueExportFunc = null, Func<string, T> valueImportFunc = null)
        {
            _valueExportFunc = valueExportFunc;
            _valueImportFunc = valueImportFunc;
            _order = data.PropertyOrDefault(() => Order, (byte)1);
            _root = new Transition();

            FromVectorDocument(data);
        }

        public void Merge(IDiscreteMarkovChain<T> other)
        {
            if (!(other is DiscreteMarkovChain<T>))
            {
                throw new ArgumentException("Incompatible items");
            }

            Merge(((DiscreteMarkovChain<T>)other)._root, _root);
        }

        public void Prune(int maxElements)
        {
            if (_root.Following.Count > maxElements)
            {
                foreach (var key in _root.Following.OrderBy(x => x.Value.Frequency).Select(v => v.Key).ToList())
                {
                    _root.Following.Remove(key);

                    if (_root.Following.Count <= maxElements)
                    {
                        break;
                    }
                }
            }
        }

        public int Order { get { return _order; } }

        public void AnalyseSequences<S>(IEnumerable<S> sequences) where S : IEnumerable<T>
        {
            //AnalyseSequencesParallel(sequences);

            foreach (var seq in sequences) AnalyseSequence(seq);
        }

        public void AnalyseSequences(IEnumerable<T> sequence, Func<T, bool> delimiter)
        {
            AnalyseSequences(sequence.Delimit(delimiter));
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

        public IDictionary<T, int> GetPriorFrequencies(T currentState)
        {
            var results = new Dictionary<T, int>();

            if (currentState != null)
            {
                foreach (var match in _root
                    .Following
                    .Select(p =>
                    {
                        Transition tx;
                        var m = p.Value.Following.TryGetValue(currentState, out tx);
                        return new
                        {
                            p = p,
                            freq = m ? tx.Frequency : 0
                        };
                    })
                    .Where(p => p.freq > 0))
                {
                    results[match.p.Key] = match.freq;
                }
            }

            return results;
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

        public XDocument ExportAsXml()
        {
            var ve = _valueExportFunc ?? new GenericTypeConverter<T>().ConvertToString;
            var doc = new XDocument(new XElement("chain", new XAttribute("order", _order)));
            var content = _root.ExportAsXml(ve).Root;
            doc.Root.Add(content);
            return doc;
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var ve = _valueExportFunc ?? new GenericTypeConverter<T>().ConvertToString;

            var doc = new BinaryVectorDocument();

            doc.SetPropertyFromExpression(() => Order);

            doc.Children.Add(_root.ExportAsBinaryVectorDoc(ve));

            return doc;
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            var vi = _valueImportFunc ?? new GenericTypeConverter<T>().ConvertFromString;

            if (!doc.Children.Any())
            {
                return;
            }

            _root.ImportBinaryVectorDoc(vi, doc.Children.First());
        }

        private void AnalyseSequencesParallel<S>(IEnumerable<S> sequences) where S : IEnumerable<T>
        {
            int i = 0;

            var chains = Enumerable.Range(1, Environment.ProcessorCount)
                .Select(n => new
                {
                    queue = new ConcurrentQueue<IEnumerable<T>>(),
                    chain = new DiscreteMarkovChain<T>(_order),
                    complete = new CancellationTokenSource()
                })
                .ToList();

            var workers = chains
                .Select(c => Task.Factory.StartNew(() =>
                {
                    while (!c.complete.IsCancellationRequested)
                    {
                        IEnumerable<T> next;

                        while (c.queue.TryDequeueWhenAvailable(out next))
                        {
                            if (next != null) c.chain.AnalyseSequence(next);
                        }
                    }
                })).ToArray();

            foreach (var seq in sequences)
            {
                var worker = chains[i % chains.Count];

                worker.queue.Enqueue(seq);

                i++;
            }

            foreach (var worker in chains)
            {
                worker.complete.Cancel();
                worker.queue.Close();
            }

            Task.WaitAll(workers);

            foreach (var worker in chains)
            {
                Merge(worker.chain);
            }

            //sequences
            //    .AsParallel()
            //    .WithDegreeOfParallelism(chains.Count)
            //    .ForAll(s =>
            //    {
            //        var n = Interlocked.Increment(ref i);

            //        var chain = chains[n % chains.Count];

            //        chain.AnalyseSequence(s);
            //    });

            //foreach (var chain in chains)
            //{
            //    Merge(chain);
            //}
        }

        private void Merge(Transition source, Transition target)
        {
            foreach (var item in source.Following)
            {
                Transition tartt;

                if (target.Following.TryGetValue(item.Key, out tartt))
                {
                    tartt.Frequency += item.Value.Frequency;
                }
                else
                {
                    target.Following[item.Key] = tartt = new Transition(item.Value.State)
                    {
                        Frequency = item.Value.Frequency
                    };
                }

                Merge(item.Value, tartt);
            }
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
            private bool _isRoot;

            public Transition() : this(default(T))
            {
                _isRoot = true;
            }

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

            public XDocument ExportAsXml(Func<T, string> valueExportFunc)
            {
                var doc = new XDocument(new XElement("tx", 
                    new XAttribute("freq", Frequency)));

                if (!_isRoot)
                {
                    doc.Root.Add(new XAttribute("v", valueExportFunc(State)));
                }

                foreach (var item in Following)
                {
                    doc.Root.Add(item.Value.ExportAsXml(valueExportFunc).Root);
                }

                return doc;
            }

            internal BinaryVectorDocument ExportAsBinaryVectorDoc(Func<T, string> valueExportFunc)
            {
                var doc = new BinaryVectorDocument();

                if (!_isRoot)
                {
                    doc.Properties["State"] = valueExportFunc(State);
                    doc.SetPropertyFromExpression(() => Frequency);
                }

                foreach (var item in Following)
                {
                    doc.Children.Add(item.Value.ExportAsBinaryVectorDoc(valueExportFunc));
                }

                return doc;
            }

            internal void ImportBinaryVectorDoc(Func<string, T> valueImportFunc, BinaryVectorDocument doc)
            {
                if (!_isRoot)
                {
                    State = valueImportFunc(doc.Properties["State"]);
                    Frequency = doc.PropertyOrDefault(() => Frequency, 0);
                }

                foreach (var child in doc.Children)
                {
                    var tx = new Transition()
                    {
                        _isRoot = false
                    };

                    tx.ImportBinaryVectorDoc(valueImportFunc, child);

                    Following[tx.State] = tx;
                }
            }

            public IDictionary<T, Transition> Following { get; private set; }
        }
    }
}