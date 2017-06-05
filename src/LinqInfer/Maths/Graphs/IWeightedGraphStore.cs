using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public interface IWeightedGraphStore<T, C>
        where T : IEquatable<T>
        where C : IComparable<C>
    {
        Task<bool> DeleteAllDataAsync();
        Task<bool> CreateOrUpdateVertexAsync(T label, IDictionary<T, C> edges = null);
        Task<IQueryable<T>> FindVerticesAsync(Expression<Func<T, bool>> predicate);
        Task<long> GetVerticeCountAsync();
        Task<IDictionary<T, C>> GetVertexEdgesAsync(T label);
        Task<IDictionary<string, object>> GetVertexAttributesAsync(T label);
        Task<bool> UpdateVertexAttributesAsync(T label, IDictionary<string, object> attributes);
        Task<bool> VertexExistsAsync(T label);
    }
}