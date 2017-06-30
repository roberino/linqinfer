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
        private readonly IDictionary<string, object> _cache;
        
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
            _cache = new Dictionary<string, object>();
        }

        public event EventHandler<EventArgsOf<T>> Modified;

        /// <summary>
        /// Exports the graph as a GEXF XML document
        /// </summary>
        public Task<XDocument> ExportAsGexfAsync()
        {
            return new GexfFormatter().FormatAsync(this);
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

        /// <summary>
        /// Removes all data from a graph store
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteAsync()
        {
            return await Storage.DeleteAllDataAsync();
        }

        /// <summary>
        /// Finds all vertexes matching a predicate (or all if the predicate is null)
        /// </summary>
        /// <param name="predicate">A predicate which operates on the label</param>
        /// <returns></returns>
        public async Task<IEnumerable<WeightedGraphNode<T, C>>> FindAllVertexesAsync(Expression<Func<T, bool>> predicate = null)
        {
            var items = new HashSet<WeightedGraphNode<T, C>>();

            foreach (var vertexLabel in await Storage.FindVerticesAsync(predicate ?? (_ => true)))
            {
                items.Add(await FindVertexAsync(vertexLabel, false));
            }

            foreach (var vertex in _workingData.Values)
            {
                if (!items.Contains(vertex))
                {
                    items.Add(vertex);
                }
            }

            return items;
        }
 
        /// <summary>
        /// Finds a vertex by label
        /// </summary>
        /// <param name="label">The vertex label</param>
        /// <returns>The vertex (as a task)</returns>
        public Task<WeightedGraphNode<T, C>> FindVertexAsync(T label)
        {
            return FindVertexAsync(label, true);
        }

        /// <summary>
        /// Finds a vertex by label or creates if it does not exist
        /// </summary>
        /// <param name="label">The vertex label</param>
        /// <returns>The new or existing vertex (as a task)</returns>
        public Task<WeightedGraphNode<T, C>> FindOrCreateVertexAsync(T label)
        {
            return FindOrCreateVertexAsync(label, true);
        }

        /// <summary>
        /// Saves the graph structure to the storage backing
        /// </summary>
        /// <returns></returns>
        public async Task<int> SaveAsync()
        {
            int i = 0;

            foreach (var item in _workingData.Values)
            {
                await item.SaveAsync();
                i++;
            }

            _workingData.Clear();

            return i;
        }

        /// <summary>
        /// Merges the other graph structure into this graph
        /// </summary>
        /// <param name="other">An other graph</param>
        /// <param name="weightMergeFunction">A function to merge weight values e.g. (x, y) => x + y</param>
        /// <returns>A task</returns>
        public async Task Merge(WeightedGraph<T, C> other, Func<C, C, C> weightMergeFunction)
        {
            foreach(var vertex in await other.FindAllVertexesAsync())
            {
                var match = await FindOrCreateVertexAsync(vertex.Label);

                foreach(var edge in await vertex.GetEdgesAsync())
                {
                    await match.ConnectToOrModifyWeightAsync(edge.Value.Label, edge.Weight, x => weightMergeFunction(x, edge.Weight));
                }
            }

            await SaveAsync();
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

        internal IDictionary<string, object> Cache { get { return _cache; } }

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

            _cache.Clear();

            Modified?.Invoke(this, new EventArgsOf<T>(label));
        }
    }
}