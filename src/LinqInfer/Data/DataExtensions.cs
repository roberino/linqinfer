using LinqInfer.Data.Orm;
using System.Data;
using System.Linq;

namespace LinqInfer.Data
{
    public static class DataExtensions
    {
        /// <summary>
        /// Returns an enumeration of a specified type. The type should map directly to the 
        /// result set returned by the query text. If no query text is supplied, it will
        /// be assumed that the type name maps directly to a table.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="connection">A database connection</param>
        /// <param name="queryText">The query text (optional)</param>
        /// <returns>An in memory enumeration of rows mapped as the specified type returned from the query</returns>
        public static IQueryable<T> Query<T>(this IDbConnection connection, string queryText = null) where T : new()
        {
            using (var mapper = new RelationalDataMapper(connection))
            {
                var data = mapper.Query<T>(queryText, CommandType.Text);

                return data.ToList().AsQueryable();
            }
        }
    }
}