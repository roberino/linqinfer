using System;
using System.Reflection;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Learning.Features
{
    public class PropertyExtractor<T>
    {
        readonly Func<T, object> _valueAccessor;
        readonly Func<object, double> _conversionFunction;

        public PropertyExtractor(int index, PropertyInfo property, FeatureAttribute featureAttribute, Func<object, double> conversionFunction = null)
        {
            Index = index;
            Property = property;
            FeatureMetadata = featureAttribute;

            _conversionFunction = conversionFunction;

            _valueAccessor = x => Property.GetValue(x);

            _valueAccessor = $"x => x.{Property.Name}".AsExpression<T, object>().Compile();
        }

        public int Index { get; internal set; }
        public PropertyInfo Property { get; }
        public FeatureAttribute FeatureMetadata { get; }
        public bool HasValue => _conversionFunction != null;

        public double GetVectorValue(T item)
        {
            var value = _valueAccessor(item);

            return _conversionFunction?.Invoke(value) ?? 0;
        }

        public object GetValue(T item)
        {
            var value = _valueAccessor(item);

            return _conversionFunction?.Invoke(value) ?? value;
        }
    }
}