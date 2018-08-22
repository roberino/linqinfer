using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    public sealed class PropertySelector<T> where T : class
    {
        internal PropertySelector()
        {
            SelectedProperties = new List<string>();
        }

        public PropertySelector<T> Select<P>(Expression<Func<T, P>> propertyExpression)
        {
            SelectedProperties.Add(LinqExtensions.GetPropertyName(propertyExpression));

            return this;
        }

        internal IList<string> SelectedProperties { get; }
    }
}