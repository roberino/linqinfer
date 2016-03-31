using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Math
{
    public static class It
    {
        public static Expression<Func<T, bool>> Is<T>(T item) where T : class
        {
            return x => x == item;
        }

        public static Expression<Func<T, bool>> IsNot<T>(T item) where T : class
        {
            return x => x != item;
        }

        public static Expression<Func<T, bool>> IsAny<T>(params Expression<Func<T, bool>>[] predicates)
        {
            if (!predicates.Any()) return x => true;

            Expression<Func<T, bool>> pnext = predicates.First();

            foreach (var p in predicates.Skip(1))
            {
                pnext = pnext.DisjunctiveJoin(p);
            }

            return pnext;
        }

        public static Expression<Func<T, bool>> IsIn<T>(params T[] values)
        {
            var subset = new HashSet<T>(values);

            return x => subset.Contains(x);
        }
    }
}
