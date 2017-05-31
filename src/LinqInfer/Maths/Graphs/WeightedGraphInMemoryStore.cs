using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public sealed class WeightedGraphInMemoryStore<T, C> : IWeightedGraphStore<T, C> where T : IEquatable<T> where C : IComparable<C>
    {
        private readonly IDictionary<T, IDictionary<T, C>> _allNodes;

        public WeightedGraphInMemoryStore()
        {
            _allNodes = new Dictionary<T, IDictionary<T, C>>();
        }

        public Task<bool> DeleteAllData()
        {
            _allNodes.Clear();

            return Task.FromResult(true);
        }

        public Task<IQueryable<T>> FindVertices(Expression<Func<T, bool>> predicate)
        {
            var func = predicate.Compile();

            return Task.FromResult(_allNodes.Keys.Where(func).AsQueryable());
        }

        public Task<long> GetVerticeCount()
        {
            return Task.FromResult((long)_allNodes.Count);
        }

        public Task<IDictionary<T, C>> ResolveVertexEdges(T label)
        {
            return Task.FromResult(_allNodes[label]);
        }

        public Task<bool> VertexExists(T label)
        {
            return Task.FromResult(_allNodes.ContainsKey(label));
        }

        public Task<bool> CreateOrUpdateVertex(T label, IDictionary<T, C> edges = null)
        {
            _allNodes[label] = edges ?? (new Dictionary<T, C>());

            return Task.FromResult(true);
        }
    }
}