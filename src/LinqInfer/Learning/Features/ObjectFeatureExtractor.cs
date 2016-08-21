using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Learning.Features
{
    internal class ObjectFeatureExtractor
    {
        private static readonly IDictionary<Type, IValueConverter> _converters;

        static ObjectFeatureExtractor()
        {
#if NET_STD
            var type = typeof(ObjectFeatureExtractor)
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
            var type = typeof(ObjectFeatureExtractor);

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
            return CreateFeatureExtractor<T>(false, setName).ExtractVector;
        }

        public IFloatingPointFeatureExtractor<T> CreateFeatureExtractor<T>(bool normaliseData = true, string setName = null) where T : class
        {
            return CreateFeatureExtractor<T>(typeof(T), normaliseData, setName);
        }

        public IFloatingPointFeatureExtractor<T> CreateFeatureExtractor<T>(Type actualType, bool normaliseData = true, string setName = null) where T : class
        {
            int i = 0;

#if NET_STD
            var type = actualType.GetTypeInfo();
#else
            var type = actualType;
#endif

            var featureProps = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(p =>
                {
                    var featureDef = p.GetCustomAttributes<FeatureAttribute>().FirstOrDefault(a => setName == null || a.SetName == setName);

                    return new
                    {
                        property = p,
                        featureDef = featureDef ?? new FeatureAttribute()
                    };
                })
                .Where(f => !f.featureDef.Ignore)
                .OrderBy(f => f.featureDef.IndexOrder)
                .ThenBy(f => f.property.Name)
                .Select(f => new { converter = CreateConverter<T>(f.property, f.featureDef), feature = f, index = i++ })
                .Where(c => c.converter != null)
                .ToList();

            return new DelegatingFloatingPointFeatureExtractor<T>((x) =>
                featureProps.Select(c => x == null ? 1f : c.converter(x)).ToArray(),
                featureProps.Count,
                normaliseData,
                featureProps.Select(f => new Feature()
                {
                    Key = f.feature.property.Name.ToLower(),
                    DataType = Type.GetTypeCode(f.feature.property.PropertyType),
                    Label = f.feature.property.Name,
                    Index = f.index,
                    Model = f.feature.featureDef.Model
                }).ToArray());
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
                    converter = _converters.Values.FirstOrDefault(c => c.CanConvert(prop.PropertyType));
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
