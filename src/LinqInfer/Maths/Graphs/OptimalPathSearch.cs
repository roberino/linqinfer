using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    /// <summary>
    /// Implementation of Dijkstras algorithm
    /// </summary>
    public sealed class OptimalPathSearch<T, C> where T : IEquatable<T> where C : IComparable<C>
    {
        readonly IDictionary<T, IDictionary<IComparer<C>, IDictionary<T, Path>>> _searchData;
        readonly WeightedGraph<T, C> _graph;
        readonly Func<C, C, C> _weightAccumulator;

        internal OptimalPathSearch(WeightedGraph<T, C> graphRoot, Func<C, C, C> weightAccumulator)
        {
            _searchData = new Dictionary<T, IDictionary<IComparer<C>, IDictionary<T, Path>>>();
            _graph = graphRoot;
            _weightAccumulator = weightAccumulator;
        }


        public Task<IEnumerable<KeyValuePair<T, C>>> FindBestPathAsync(T start, T end, Func<C, C, int> compareFunc)
        {
            return FindBestPathAsync(start, end, new DynamicComparer<C>(compareFunc));
        }

        public async Task<IEnumerable<KeyValuePair<T, C>>> FindBestPathAsync(T start, T end, IComparer<C> costComparer = null)
        {
            var startNode = await _graph.FindVertexAsync(start);

            return await FindBestPathAsync(startNode, end, costComparer);
        }

        public void ClearCache()
        {
            _searchData.Clear();
        }

        public void ClearCache(T label)
        {
            if (_searchData.ContainsKey(label)) _searchData.Remove(label);
        }

        internal async Task<IEnumerable<KeyValuePair<T, C>>> FindBestPathAsync(WeightedGraphNode<T, C> start, T end, IComparer<C> costComparer = null)
        {
            var paths = new List<KeyValuePair<T, C>>();
            var next = end;

            if (costComparer == null) costComparer = Comparer<C>.Default;

            IDictionary<IComparer<C>, IDictionary<T, Path>> cacheData;
            IDictionary<T, Path> costData;

            if (!_searchData.TryGetValue(start.Label, out cacheData) || !cacheData.TryGetValue(costComparer, out costData))
            {
                costData = await CalculateAsync(start, costComparer);
            }

            while (next != null)
            {
                var path = costData[next];

                paths.Add(new KeyValuePair<T, C>(path.Vertex, path.Cost));

                next = path.PreviousVertex;
            }

            paths.Reverse();

            return paths;
        }

        IDictionary<IComparer<C>, IDictionary<T, Path>> GetDataCache(T key)
        {
            IDictionary<IComparer<C>, IDictionary<T, Path>> cache = null;

            if (!_searchData.TryGetValue(key, out cache))
            {
                _searchData[key] = cache = new Dictionary<IComparer<C>, IDictionary<T, Path>>();
            }

            return cache;
        }

        async Task<IDictionary<T, Path>> CalculateAsync(WeightedGraphNode<T, C> start, IComparer<C> costComparer)
        {
            C currentCost = default(C);

            var cache = GetDataCache(start.Label);

            var costData = cache[costComparer] = new Dictionary<T, Path>();

            while (true)
            {
                if (costData.ContainsKey(start.Label))
                {
                    costData[start.Label].Visited = true;
                }
                else
                {
                    costData[start.Label] = new Path() { Vertex = start.Label, Visited = true };
                }

                foreach (var child in await start.GetEdgesAsync())
                {
                    Path path;

                    var newCost = _weightAccumulator(child.Weight, currentCost);

                    if (!costData.TryGetValue(child.Value.Label, out path))
                    {
                        costData[child.Value.Label] = path = new Path()
                        {
                            PreviousVertex = start.Label,
                            Vertex = child.Value.Label,
                            Cost = newCost
                        };
                    }

                    if (newCost.CompareTo(path.Cost) < 0)
                    {
                        path.Cost = newCost;
                        path.PreviousVertex = start.Label;
                    }
                }

                var best = costData
                    .Where(c => !c.Value.Visited)
                    .Join(start.Edges, o => o.Key, i => i.Value.Label, (o, i) => o)
                    .OrderBy(x => x.Value.Cost, costComparer)
                    .FirstOrDefault();

                if (best.Value == null)
                {
                    break;
                }

                start = start.Edges.First(c => c.Value.Label.Equals(best.Key)).Value;
                currentCost = best.Value.Cost;
            }

            return costData;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var cache in _searchData)
            {
                sb.AppendLine($"{cache.Key}");

                foreach (var costs in cache.Value)
                {
                    sb.AppendLine($"{costs.Key}");
                    sb.AppendLine("===================================");
                    sb.AppendLine("Vertex\t|\tCost\t|\tPrevious Vertex");

                    foreach (var item in costs.Value)
                    {
                        sb.AppendLine($"{item.Value.Vertex}\t|\t{item.Value.Cost}\t|\t{item.Value.PreviousVertex}");
                    }
                }
            }

            return sb.ToString();
        }

        class Path
        {
            public T PreviousVertex { get; set; }
            public T Vertex { get; set; }
            public bool Visited { get; set; }
            public C Cost { get; set; }
        }
    }
}