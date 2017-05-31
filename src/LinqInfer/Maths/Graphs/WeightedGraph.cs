using LinqInfer.Data;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public class WeightedGraph<T, C> where T : IEquatable<T> where C : IComparable<C>
    {
        private readonly Lazy<OptimalPathSearch<T, C>> _pathSearch;
        private readonly ConcurrentDictionary<T, WeightedGraphNode<T, C>> _workingData;
        
        public WeightedGraph(IWeightedGraphStore<T, C> store, Func<C, C, C> weightAccumulator)
        {
            Storage = store;

            _pathSearch = new Lazy<OptimalPathSearch<T, C>>(
                () => new OptimalPathSearch<T, C>(this, weightAccumulator ?? ((x, y) =>
            {
                dynamic xd = x;
                dynamic xy = y;
                return (C)(xd + xy);
            })));

            _workingData = new ConcurrentDictionary<T, WeightedGraphNode<T, C>>();
        }

        public event EventHandler<EventArgsOf<T>> Modified;

        public OptimalPathSearch<T, C> OptimalPathSearch
        {
            get
            {
                return _pathSearch.Value;
            }
        }

        public long VerticeCount
        {
            get
            {
                return Storage.GetVerticeCount().Result;
            }
        }

        public WeightedGraphNode<T, C> this[T label]
        {
            get
            {
                return FindVertexAsync(label).Result;
            }
        }

        public async Task<bool> DeleteAsync()
        {
            return await Storage.DeleteAllData();
        }
 
        public Task<WeightedGraphNode<T, C>> FindVertexAsync(T label)
        {
            return FindVertexAsync(label, true);
        }

        public async Task<int> SaveAsync()
        {
            int i = 0;

            foreach(var item in _workingData.Values)
            {
                await item.SaveAsync();
                i++;
            }

            _workingData.Clear();

            return i;
        }

        public Task<WeightedGraphNode<T, C>> FindOrCreateVertexAsync(T label)
        {
            return FindOrCreateVertexAsync(label, true);
        }

        internal async Task<WeightedGraphNode<T, C>> FindVertexAsync(T label, bool validateIntegrity)
        {
            WeightedGraphNode<T, C> vertex = null;

            if (!_workingData.TryGetValue(label, out vertex))
            {
                if (!validateIntegrity || await Storage.VertexExists(label))
                {
                    vertex = new WeightedGraphNode<T, C>(this, label);
                }
            }

            return vertex;
        }

        internal IWeightedGraphStore<T, C> Storage { get; private set; }

        internal async Task<WeightedGraphNode<T, C>> FindOrCreateVertexAsync(T label, bool fireEvent)
        {
            WeightedGraphNode<T, C> vertex = null;

            if (!_workingData.TryGetValue(label, out vertex))
            {
                if (!(await Storage.VertexExists(label)))
                {
                    vertex = new WeightedGraphNode<T, C>(this, label, true);

                    _workingData[label] = vertex;

                    if (fireEvent) OnModify(label);
                }
                else
                {
                    vertex = new WeightedGraphNode<T, C>(this, label, false);
                }
            }

            return vertex;
        }

        internal void RegisterChange(WeightedGraphNode<T, C> vertex)
        {
            _workingData[vertex.Label] = vertex;
        }

        internal void OnModify(T label)
        {
            if (_pathSearch.IsValueCreated)
            {
                _pathSearch.Value.ClearCache();
            }

            Modified?.Invoke(this, new EventArgsOf<T>(label));
        }
    }
}