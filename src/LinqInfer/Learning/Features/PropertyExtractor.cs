using System;
using System.Reflection;

namespace LinqInfer.Learning.Features
{
    public class PropertyExtractor<T>
    {
        public PropertyExtractor()
        {
        }

        public PropertyExtractor(int index, PropertyInfo property, FeatureAttribute featureAttribute, Func<T, double> conversionFunction = null)
        {
            Index = index;
            Property = property;
            FeatureMetadata = featureAttribute;
            ConversionFunction = conversionFunction;
        }

        public int Index { get; internal set; }
        public PropertyInfo Property { get;}
        public FeatureAttribute FeatureMetadata { get; }
        public Func<T, double> ConversionFunction { get; }
    }
}