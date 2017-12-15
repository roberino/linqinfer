using LinqInfer.Data.Pipes;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Utility
{
    public static class LinqExtensions
    {
        /// <summary>
        /// Converts an enumeration into an async enumerator
        /// </summary>
        public static IAsyncEnumerator<T> AsAsyncEnumerator<T>(this IEnumerable<T> values, int batchSize = 1000)
        {
            return From.Enumerable(values, batchSize);
        }

        /// <summary>
        /// Converts an enumeration of batch loading tasks
        /// into an async enumerable object
        /// </summary>
        /// <typeparam name="T">The type of each item in a batch of data</typeparam>
        /// <param name="batchLoader">An enumeration of tasks to load data</param>
        public static IAsyncEnumerator<T> AsAsyncEnumerator<T>(this IEnumerable<Task<IList<T>>> batchLoader)
        {
            return From.EnumerableTasks(batchLoader);
        }

        /// <summary>
        /// Iterates over an enumeration of values and applies a function returning the results of the function
        /// </summary>
        public static IList<O> ForEach<I, O>(this IEnumerable<I> values, Func<I, O> func)
        {
            var len = values is IList<I> ? ((IList<I>)values).Count : 16;
            var results = new List<O>(len);

            foreach (var v in values)
            {
                results.Add(func(v));
            }

            return results;
        }

        /// <summary>
        /// Zips together two enumerables, returning when the end of both have been reached. If one enumerable contains less items
        /// than the other then the default value is returned. E.g.
        /// [1,2,3].ZipAll([1,2], (x,y) => [x,y]) = [[1,1],[2,2],[3,0]]
        /// OR using references:
        /// ["1","2","3"].ZipAll(["1","2"], (x,y) => [x,y]) = [["1","1"],["2","2"],["3",null]]
        /// </summary>
        public static IEnumerable<O> ZipAll<T1, T2, O>(this IEnumerable<T1> items1, IEnumerable<T2> items2, Func<T1, T2, O> resultSelector)
        {
            var e1 = items1.GetEnumerator();
            var e2 = items2.GetEnumerator();
            var e1cont = true;
            var e2cont = true;

            T1 item1;
            T2 item2;

            try
            {
                while (e1cont || e2cont)
                {
                    if (e1cont)
                    {
                        e1cont = e1.MoveNext();
                        item1 = e1cont ? e1.Current : default(T1);
                    }
                    else
                    {
                        item1 = default(T1);
                    }

                    if (e2cont)
                    {
                        e2cont = e2.MoveNext();
                        item2 = e2cont ? e2.Current : default(T2);
                    }
                    else
                    {
                        item2 = default(T2);
                    }

                    if (e1cont || e2cont) yield return resultSelector(item1, item2);
                }
            }
            finally
            {
                e1.Dispose();
                e2.Dispose();
            }
        }

        /// <summary>
        /// Returns a distinct list of items using the comparison functions
        /// </summary>
        /// <typeparam name="T">The type of item</typeparam>
        /// <param name="compareFunc">A function returning true is two objects are considered equal</param>
        /// <param name="hashCodeFunc">A function returning a hashcode (default will be used if not supplied)</param>
        /// <returns>A distinct list of T</returns>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items, Func<T, T, bool> compareFunc, Func<T, int> hashCodeFunc = null)
        {
            var comparer = CreateComparer<T>(compareFunc, hashCodeFunc);

            return items.Distinct(comparer);
        }

        /// <summary>
        /// Creates an equality comparer from a comparing function.
        /// If the type is class then a null value will always compare as false.
        /// </summary>
        /// <typeparam name="T">The type of item</typeparam>
        /// <param name="compareFunc">A function returning true is two objects are considered equal</param>
        /// <param name="hashCodeFunc">A function returning a hashcode (default will be used if not supplied)</param>
        /// <returns>An <see cref="IEqualityComparer{T}"/> instance</returns>
        public static IEqualityComparer<T> CreateComparer<T>(Func<T, T, bool> compareFunc, Func<T, int> hashCodeFunc = null)
        {
            return new DynamicEqualityComparer<T>(compareFunc, hashCodeFunc);
        }

        /// <summary>
        /// Splits an enumeration into subsets
        /// using a delimiting function.
        /// </summary>
        /// <typeparam name="T">The type of item</typeparam>
        /// <param name="items">An enumerable sequence of items</param>
        /// <param name="delimitingFunction">A function which will return true value to indicate a delimiting item</param>
        /// <returns>A enumerable of enumerable items</returns>
        public static IEnumerable<IEnumerable<T>> Delimit<T>(this IEnumerable<T> items, Func<T, bool> delimitingFunction)
        {
            Contract.Assert(delimitingFunction != null);

            var batch = new List<T>();

            foreach (var item in items)
            {
                if (delimitingFunction(item))
                {
                    yield return batch.ToArray();
                    batch.Clear();
                }
                batch.Add(item);
            }

            if (batch.Any()) yield return batch;
        }

        /// <summary>
        /// Orders an enumeration of values randomly.
        /// </summary>
        /// <param name="source">The source items</param>
        /// <returns></returns>
        public static IOrderedEnumerable<T> RandomOrder<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(_ => Guid.NewGuid());
        }

        internal static string GetPropertyName<TSource, TField>(Expression<Func<TSource, TField>> propertyExpression)
        {
            return (propertyExpression.Body as MemberExpression ?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression).Member.Name;
        }

        internal static string GetPropertyName<TField>(Expression<Func<TField>> propertyExpression)
        {
            return (propertyExpression.Body as MemberExpression ?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression).Member.Name;
        }

        /// <summary>
        /// Inverts an expression e.g. not(exp).
        /// </summary>
        public static Expression<Func<T, bool>> Invert<T>(this Expression<Func<T, bool>> exp)
        {
            var notExp = Expression.Not(exp.Body);

            return Expression.Lambda<Func<T, bool>>(notExp, exp.Parameters);
        }

        /// <summary>
        /// Joins two expressions with an OR.
        /// </summary>
        public static Expression<Func<T, bool>> DisjunctiveJoin<T>(this Expression<Func<T, bool>> exp1, Expression<Func<T, bool>> exp2)
        {
            var exp2p = UpdateParameter(exp2, exp1.Parameters.First());
            var andExp = Expression.OrElse(exp1.Body, exp2p.Body);

            return Expression.Lambda<Func<T, bool>>(andExp, exp1.Parameters);
        }

        /// <summary>
        /// Joins two expressions with an AND.
        /// </summary>
        public static Expression<Func<T, bool>> ConjunctiveJoin<T>(this Expression<Func<T, bool>> exp1, Expression<Func<T, bool>> exp2)
        {
            var exp2p = UpdateParameter(exp2, exp1.Parameters.First());
            var andExp = Expression.AndAlso(exp1.Body, exp2p.Body);

            return Expression.Lambda<Func<T, bool>>(andExp, exp1.Parameters);
        }

        /// <summary>
        /// Chunks up a queryable source into batches.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IQueryable<T> source, int size = 1000)
        {
            Contract.Requires(size > 0);

            IList <T> batch;
            int next = 0;

            while (true)
            {
                batch = source.Skip(next).Take(size).ToList();

                if (batch.Any()) yield return batch;

                next += size;

                if (batch.Count < size)
                {
                    break;
                }
            }
        }

        static Expression<Func<T, bool>> UpdateParameter<T>(
            Expression<Func<T, bool>> expr,
            ParameterExpression newParameter)
        {
            var visitor = new ParameterUpdateVisitor(expr.Parameters[0], newParameter);
            var body = visitor.Visit(expr.Body);

            return Expression.Lambda<Func<T, bool>>(body, newParameter);
        }

        class ParameterUpdateVisitor : ExpressionVisitor
        {
            private ParameterExpression _oldParameter;
            private ParameterExpression _newParameter;

            public ParameterUpdateVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (ReferenceEquals(node, _oldParameter))
                    return _newParameter;

                return base.VisitParameter(node);
            }
        }
    }
}
