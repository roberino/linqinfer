using LinqInfer.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public Task<XDocument> ExportAsGefxAsync()
        {
            return new GefxFormatter().FormatAsync(this);
        }

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
                return Storage.GetVerticeCountAsync().Result;
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
            return await Storage.DeleteAllDataAsync();
        }

        public async Task<IEnumerable<WeightedGraphNode<T, C>>> FindAllVertexesAsync(Expression<Func<T, bool>> predicate = null)
        {
            var items = new List<WeightedGraphNode<T, C>>();

            foreach (var vertexLabel in await Storage.FindVerticesAsync(predicate ?? (_ => true)))
            {
                items.Add(await FindVertexAsync(vertexLabel, false));
            }

            return items;
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
                if (!validateIntegrity || await Storage.VertexExistsAsync(label))
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
                if (!(await Storage.VertexExistsAsync(label)))
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