using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Learning.Features
{
    internal class ObjectFeatureExtractorFactory
    {
        private static readonly IDictionary<Type, IValueConverter> _converters;

        static ObjectFeatureExtractorFactory()
        {
#if NET_STD
            var type = typeof(ObjectFeatureExtractorFactory)
                    .GetTypeInfo();

            _converters =
                type
                    .Assembly
                    .ExportedTypes
                    .Select(t => t.GetTypeInfo())
                    .Where(t =>
                        t.IsPublic && t.GetConstructor(new Type[0]) != null && t.GetInterfaces()
                            .Any(i => i == typeof(IValueConverter)))
                    .ToDictionary(x => x.AsType(), x => (IValueConverter)Activator.CreateInstance(x.AsType()));
#else
            var type = typeof(ObjectFeatureExtractorFactory);

            _converters =
                type
                    .Assembly
                    .ExportedTypes
                    .Where(t =>
                        t.IsPublic && t.GetConstructor(new Type[0]) != null && t.GetInterfaces()
                            .Any(i => i == typeof(IValueConverter)))
                    .ToDictionary(x => x, x => (IValueConverter)Activator.CreateInstance(x));
#endif
        }

        public Func<T, double[]> CreateFeatureExtractorFunc<T>(string setName = null) where T : class
        {
            return CreateFeatureExtractor<T>(setName).ExtractVector;
        }

        public IFloatingPointFeatureExtractor<T> CreateFeatureExtractor<T>(string setName = null) where T : class
        {
            return CreateFeatureExtractor<T>(typeof(T), setName);
        }

        public IFloatingPointFeatureExtractor<T> CreateFeatureExtractor<T>(Type actualType, string setName = null) where T : class
        {
            var featureProperties = GetFeatureProperties<T>(actualType, setName)
                .Where(f => f.ConversionFunction != null).ToList();

            DebugOutput.Log($"found {featureProperties.Count} features for type {actualType.FullName}");

            var i = 0;
            foreach (var p in featureProperties) p.Index = i++;

            return new DelegatingFloatingPointFeatureExtractor<T>((x) =>
                featureProperties.Select(c => x == null ? 1f : c.ConversionFunction(x)).ToArray(),
                featureProperties.Count,
                featureProperties.Select(f => new Feature()
                {
                    Key = f.Property.Name.ToLower(),
                    DataType = Type.GetTypeCode(f.Property.PropertyType),
                    Label = f.Property.Name,
                    Index = f.Index,
                    Model = f.FeatureMetadata.Model
                }).ToArray());
        }

        internal IList<PropertyExtractor<T>> GetFeatureProperties<T>(Type actualType, string setName = null) where T : class
        {
            int i = 0;

#if NET_STD
            var type = actualType.GetTypeInfo();
#else
            var type = actualType;
#endif

            return type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .Select(p =>
                {
                    var featureDef = new FeatureAttribute();
                    var featureAttr = p.GetCustomAttributes<FeatureAttribute>().FirstOrDefault(a => setName == null || a.SetName == setName);

                    if (featureAttr != null)
                    {
                        featureDef.Converter = featureAttr.Converter;
                        featureDef.Ignore = featureAttr.Ignore;
                        featureDef.IndexOrder = featureAttr.IndexOrder;
                        featureDef.Model = featureAttr.Model;
                        featureDef.SetName = featureAttr.SetName;
                    }

                    return new
                    {
                        property = p,
                        featureDef = featureDef
                    };
                })
                .Where(f => !f.featureDef.Ignore)
                .OrderBy(f => f.featureDef.IndexOrder)
                .ThenBy(f => f.property.Name)
                .Select(f => new PropertyExtractor<T>(i++, f.property, f.featureDef, CreateConverter<T>(f.property, f.featureDef)))
                .ToList();
        }

        private Func<T, double> CreateConverter<T>(PropertyInfo prop, FeatureAttribute featureDef)
        {
            IValueConverter converter = null;

            if (featureDef != null)
            {
                if (featureDef.Converter != null)
                {
                    if (!_converters.TryGetValue(featureDef.Converter, out converter))
                    {
                        lock (_converters)
                        {
                            _converters[featureDef.Converter] = converter = (IValueConverter)Activator.CreateInstance(featureDef.Converter);
                        }
                    }
                }
                else
                {
                    converter = _converters.Values.FirstOrDefault(c => c is IDefaultValueConverter && c.CanConvert(prop.PropertyType));
                }
            }
            else
            {
                converter = _converters.Values.FirstOrDefault(c => c.CanConvert(prop.PropertyType));
            }

            if (converter == null) return null;

            return x => converter.Convert(prop.GetValue(x));
        }
    }
}
