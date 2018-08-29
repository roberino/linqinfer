using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Learning.Features
{
    class ObjectFeatureExtractor<T> : BaseFeatureExtractor<T>
    {
        static readonly IDictionary<Type, IValueConverter> _converters;
        static readonly ConcurrentDictionary<string, IList<PropertyExtractor<T>>> _extractors;

        static ObjectFeatureExtractor()
        {
            var type = typeof(ObjectFeatureExtractor<>);

            _extractors = new ConcurrentDictionary<string, IList<PropertyExtractor<T>>>();

            _converters =
                type
                    .Assembly
                    .ExportedTypes
                    .Where(t =>
                        t.IsPublic && t.GetConstructor(new Type[0]) != null && t.GetInterfaces()
                            .Any(i => i == typeof(IValueConverter)))
                    .ToDictionary(x => x, x => (IValueConverter)Activator.CreateInstance(x));
        }

        public ObjectFeatureExtractor(string setName = null) : base(CreateFeatureExtractor(setName))
        {
            SetName = setName;
        }

        public string SetName { get; }

        public override PortableDataDocument ExportData()
        {
            var doc = base.ExportData();

            doc.SetPropertyFromExpression(() => SetName);

            return doc;
        }

        public static IFloatingPointFeatureExtractor<T> Create(PortableDataDocument data)
        {
            return new ObjectFeatureExtractor<T>(data.PropertyOrDefault(nameof(SetName), string.Empty));
        }

        public static IList<PropertyExtractor<T>> GetFeatureProperties(Type type, string setName = null, bool convertableOnly = false)
        {
            var i = 0;

            var fProps = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .Select(p =>
                {
                    var featureDef = new FeatureAttribute();
                    var featureAttr = p.GetCustomAttributes<FeatureAttribute>()
                        .FirstOrDefault(a => setName == null || a.SetName == setName);

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
                        featureDef
                    };
                })
                .Where(f => !f.featureDef.Ignore)
                .OrderBy(f => f.featureDef.IndexOrder)
                .ThenBy(f => f.property.Name)
                .Select(f =>
                    new PropertyExtractor<T>(i++, f.property, f.featureDef, CreateConverter(f.property, f.featureDef)));

            if (convertableOnly)
            {
                i = 0;

                fProps = fProps.Where(f => f.HasValue);

                foreach (var item in fProps)
                {
                    item.Index = i++;
                }
            }

            var fPropsList = fProps.ToList();

            DebugOutput.Log($"found {fPropsList.Count} features for type {type.FullName}");

            return fPropsList;
        }

        static BaseFeatureExtractor<T> CreateFeatureExtractor(string setName = null)
        {
            return CreateFeatureExtractor(typeof(T), setName);
        }

        static BaseFeatureExtractor<T> CreateFeatureExtractor(Type actualType, string setName = null)
        {
            var featureProperties = _extractors
                .GetOrAdd($"{actualType.AssemblyQualifiedName}${setName}", k =>
                GetFeatureProperties(actualType, setName, true));

            return new BaseFeatureExtractor<T>(x =>
                    Extract(x, featureProperties),
                featureProperties.Count,
                featureProperties.Select(f => new Feature()
                {
                    Key = f.Property.Name.ToLower(),
                    DataType = Type.GetTypeCode(f.Property.PropertyType),
                    Label = f.Property.Name,
                    Index = f.Index,
                    Model = f.FeatureMetadata.Model
                }).Cast<IFeature>().ToArray());
        }

        static IVector Extract(T item, IList<PropertyExtractor<T>> properties)
        {
            var values = new double[properties.Count];

            if (item != null)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = properties[i].GetVectorValue(item);
                }
            }

            return new ColumnVector1D(values);
        }

        static Func<object, double> CreateConverter(PropertyInfo prop, FeatureAttribute featureDef)
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
                            _converters[featureDef.Converter] = converter =
                                (IValueConverter)Activator.CreateInstance(featureDef.Converter);
                        }
                    }
                }
                else
                {
                    converter = _converters.Values.FirstOrDefault(c =>
                        c is IDefaultValueConverter && c.CanConvert(prop.PropertyType));
                }
            }
            else
            {
                converter = _converters.Values.FirstOrDefault(c => c.CanConvert(prop.PropertyType));
            }

            if (converter == null) return null;

            return converter.Convert;
        }
    }
}