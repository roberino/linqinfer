using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public sealed class WeightedGraphInMemoryStore<T, C> : IWeightedGraphStore<T, C> where T : IEquatable<T> where C : IComparable<C>
    {
        readonly IDictionary<T, IDictionary<T, C>> _allNodes;
        readonly IDictionary<T, IDictionary<string, object>> _attributes;

        public WeightedGraphInMemoryStore()
        {
            _allNodes = new Dictionary<T, IDictionary<T, C>>();
            _attributes = new Dictionary<T, IDictionary<string, object>>();
        }

        public Task<bool> DeleteAllDataAsync()
        {
            _allNodes.Clear();

            return Task.FromResult(true);
        }

        public Task<IQueryable<T>> FindVerticesAsync(Expression<Func<T, bool>> predicate)
        {
            var func = predicate.Compile();

            return Task.FromResult(_allNodes.Keys.Where(func).AsQueryable());
        }

        public Task<long> GetVerticeCountAsync()
        {
            return Task.FromResult((long)_allNodes.Count);
        }

        public Task<IDictionary<T, C>> GetVertexEdgesAsync(T label)
        {
            return Task.FromResult(_allNodes[label]);
        }

        public Task<bool> VertexExistsAsync(T label)
        {
            return Task.FromResult(_allNodes.ContainsKey(label));
        }

        public Task<bool> CreateOrUpdateVertexAsync(T label, IDictionary<T, C> edges = null)
        {
            _allNodes[label] = edges ?? (new Dictionary<T, C>());

            return Task.FromResult(true);
        }

        public Task<IDictionary<string, object>> GetVertexAttributesAsync(T label)
        {
            IDictionary<string, object> attribs;

            if (!_attributes.TryGetValue(label, out attribs))
            {
                attribs = new Dictionary<string, object>();
            }

            return Task.FromResult(attribs);
        }

        public Task<bool> UpdateVertexAttributesAsync(T label, IDictionary<string, object> attributes)
        {
            _attributes[label] = attributes;

            return Task.FromResult(true);
        }
    }
}