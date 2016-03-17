using LinqInfer.Annotation;
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
            _converters =
                typeof(ObjectFeatureExtractor)
                    .Assembly
                    .ExportedTypes
                    .Where(t =>
                        t.IsPublic && t.GetConstructor(new Type[0]) != null && t.GetInterfaces()
                            .Any(i => i == typeof(IValueConverter)))
                    .ToDictionary(x => x, x => (IValueConverter)Activator.CreateInstance(x));
        }

        public Func<T, float[]> CreateFeatureExtractorFunc<T>(string setName = null) where T : class
        {
            return CreateFeatureExtractor<T>(false, setName).ExtractVector;
        }

        public IFloatingPointFeatureExtractor<T> CreateFeatureExtractor<T>(bool normaliseData = true, string setName = null) where T : class
        {
            return CreateFeatureExtractor<T>(typeof(T), normaliseData, setName);
        }

        public IFloatingPointFeatureExtractor<T> CreateFeatureExtractor<T>(Type actualType, bool normaliseData = true, string setName = null) where T : class
        {
            var featureProps = actualType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(p => p.Name)
                .Select(c => new { converter = CreateConverter<T>(c, setName), name = c.Name })
                .Where(c => c.converter != null)
                .ToList();

            return new DelegatingFloatingPointFeatureExtractor<T>((x) =>
                featureProps.Select(c => x == null ? 1f : c.converter(x)).ToArray(),
                featureProps.Count,
                normaliseData,
                featureProps.Select(f => f.name).ToArray());
        }

        private Func<T, float> CreateConverter<T>(PropertyInfo prop, string setName)
        {
            var featureDef = prop.GetCustomAttributes<FeatureAttribute>().FirstOrDefault(a => setName == null || a.SetName == setName);

            IValueConverter converter = null;

            if (featureDef != null)
            {
                if (featureDef.Converter != null)
                {
                    if (!_converters.TryGetValue(featureDef.Converter, out converter))
                    {
                        lock (_converters)
                        {
                            _converters[featureDef.Converter] = (IValueConverter)Activator.CreateInstance(featureDef.Converter);
                        }
                    }
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
