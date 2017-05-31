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
        Task<bool> DeleteAllData();
        Task<bool> CreateOrUpdateVertex(T label, IDictionary<T, C> edges = null);
        Task<IQueryable<T>> FindVertices(Expression<Func<T, bool>> predicate);
        Task<long> GetVerticeCount();
        Task<IDictionary<T, C>> ResolveVertexEdges(T label);
        Task<bool> VertexExists(T label);
    }
}