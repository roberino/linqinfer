using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility
{
    public static class LinqExtensions
    {
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
            Contract.Assert(size > 0);

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
