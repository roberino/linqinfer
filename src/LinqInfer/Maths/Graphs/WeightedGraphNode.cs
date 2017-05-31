using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public sealed class WeightedGraphNode<T, C> where T : IEquatable<T> where C : IComparable<C>
    {
        private readonly ReaderWriterLockSlim _lock;
        private readonly WeightedGraph<T, C> _owner;
        private readonly IDictionary<T, C> _workingEdges;

        private bool _loaded;
        private bool _isDirty;

        internal WeightedGraphNode(WeightedGraph<T, C> owner, T label, bool isNew = false)
        {
            _owner = owner;
            _workingEdges = new Dictionary<T, C>();
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _isDirty = isNew;
            _loaded = isNew;

            Label = label;
        }

        public T Label { get; private set; }

        public async Task<IEnumerable<WeightedPair<WeightedGraphNode<T, C>, C>>> GetEdgesAsync()
        {
            if (!_loaded)
            {
                _lock.EnterWriteLock();

                try
                {
                    if (!_loaded)
                    {
                        var edges = await _owner.Storage.ResolveVertexEdges(Label);

                        foreach (var edge in edges)
                        {
                            if (!_workingEdges.ContainsKey(edge.Key))
                            {
                                // TODO: Potential for overwrite of uncommited data

                                _workingEdges[edge.Key] = edge.Value;
                            }
                        }
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            var edgeTasks = _workingEdges
                .Select(async e =>
                    new WeightedPair<WeightedGraphNode<T, C>, C>((await _owner.FindVertexAsync(e.Key, false)), e.Value))
                    .ToList();

            await Task.WhenAll(edgeTasks);

            return edgeTasks.Select(e => e.Result);
        }

        public IEnumerable<WeightedPair<WeightedGraphNode<T, C>, C>> Edges { get { return GetEdgesAsync().Result; } }

        public int NumberOfEdges
        {
            get
            {
                LoadSync();
                return _workingEdges.Count;
            }
        }

        public async Task<C> GetWeightAsync(T label)
        {
            await GetEdgesAsync();

            C weight;

            if (!_workingEdges.TryGetValue(label, out weight))
            {
                throw new InvalidOperationException("Edge not defined : " + label);
            }

            return weight;
        }

        public async Task IncrementWeightAsync(T label, Func<C, C> weightIncrementFunction)
        {
            var weight = await GetWeightAsync(label);

            _workingEdges[label] = weightIncrementFunction(weight);

            _owner.RegisterChange(this);

            _isDirty = true;

            _owner.OnModify(label);
        }

        public bool IsConnectedTo(T label)
        {
            LoadSync();

            return _workingEdges.ContainsKey(label);
        }

        public async Task<WeightedGraphNode<T, C>> ConnectToOrModifyWeightAsync(T label, C initialWeight, Func<C, C> weightModifier)
        {
            await GetEdgesAsync();

            var vertex = await _owner.FindOrCreateVertexAsync(label, false);

            C weight;

            try
            {
                _lock.EnterWriteLock();

                if (!_workingEdges.TryGetValue(label, out weight))
                {
                    weight = initialWeight;
                }

                weight = weightModifier(weight);

                _workingEdges[label] = weight;

                _isDirty = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            _owner.OnModify(label);

            return vertex;
        }

        public async Task<WeightedGraphNode<T, C>> ConnectToAsync(T label, C weight)
        {
            await GetEdgesAsync();

            var vertex = await _owner.FindOrCreateVertexAsync(label, false);

            try
            {
                _lock.EnterWriteLock();

                _workingEdges[vertex.Label] = weight;

                _isDirty = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            _owner.OnModify(label);

            return vertex;
        }

        public override string ToString()
        {
            return string.Format($"{Label} ({_workingEdges.Count})");
        }

        public override int GetHashCode()
        {
            return Label.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        internal async Task SaveAsync()
        {
            if (!_isDirty) return;

            _lock.EnterWriteLock();

            try
            {
                await _owner.Storage.CreateOrUpdateVertex(Label, _workingEdges.ToDictionary(e => e.Key, e => e.Value));

                _isDirty = false;                
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void LoadSync()
        {
            if (!_loaded)
            {
                GetEdgesAsync().Wait();
            }
        }
    }
}