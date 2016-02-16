using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    public static class It
    {
        public static Expression<Func<T, bool>> Is<T>(T item)
        {
            return x => ReferenceEquals(x, item);
        }

        public static Expression<Func<T, bool>> IsAny<T>(params Func<T, bool>[] predicates)
        {
            return x => predicates.Any(p => p.Invoke(x));
        }

        public static Expression<Func<T, bool>> IsIn<T>(params T[] values)
        {
            var subset = new HashSet<T>(values);

            return x => subset.Contains(x);
        }
    }
}
