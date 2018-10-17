using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class PolymorphicFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>
    {
        readonly object _lockObj = new object();
        readonly Dictionary<Type, TypeMap> _typeMaps;
        readonly Dictionary<string, int> _indexMap;

        int _maxIndex;

        public PolymorphicFeatureExtractor(int vectorSize)
        {
            _typeMaps = new Dictionary<Type, TypeMap>();
            _indexMap = new Dictionary<string, int>();
            _maxIndex = -1;
            VectorSize = vectorSize;
        }

        public int VectorSize { get; private set; }

        public bool CapacityReached => VectorSize <= _maxIndex + 1;

        public IEnumerable<IFeature> FeatureMetadata => _typeMaps
            .SelectMany(m => m.Value.Properties)
            .GroupBy(p => p.Index)
            .Select(g => new Feature()
            {
                Index = g.Key,
                DataType = Type.GetTypeCode(g.First().Property.PropertyType),
                Key = g.First().Property.Name,
                Label = g.First().Property.Name,
                Model = g.First().FeatureMetadata.Model
            });

        public PortableDataDocument ExportData()
        {
            var data = new PortableDataDocument();

            data.SetType(GetType());

            data.SetPropertyFromExpression(() => VectorSize);

            foreach (var index in _indexMap)
            {
                data.Properties[$"_{index.Key}"] = index.Value.ToString();
            }

            return data;
        }

        public IVector ExtractIVector(T obj)
        {
            var map = GetOrCreateMap(obj.GetType());

            var data = new double[VectorSize];

            foreach (var prop in map.Properties)
            {
                data[prop.Index] = prop.GetVectorValue(obj);
            }

            return new ColumnVector1D(data);
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public void MapTypeInstance(T obj)
        {
            GetOrCreateMap(obj.GetType());
        }

        public void Resize()
        {
            VectorSize = _maxIndex + 1;
        }

        TypeMap GetOrCreateMap(Type type)
        {
            if (!_typeMaps.TryGetValue(type, out var map))
            {
                lock (_lockObj)
                {
                    _typeMaps[type] = map = CreateMap(type);
                }
            }

            return map;
        }

        TypeMap CreateMap(Type type)
        {
            var extractors = ObjectFeatureExtractor<T>.GetFeatureProperties(type);
            
            var withinRange = new List<PropertyExtractor<T>>();

            foreach (var prop in extractors)
            {
                var index = GetIndex(prop.Property.Name);

                if (index.HasValue)
                {
                    prop.Index = index.Value;
                    withinRange.Add(prop);
                }
            }

            return new TypeMap(withinRange);
        }

        int? GetIndex(string name)
        {
            if (!_indexMap.TryGetValue(name, out var index))
            {
                if (_maxIndex >= VectorSize - 1)
                {
                    return null;
                }
                
                index = ++_maxIndex;

                _indexMap[name] = index;
            }

            return index;
        }

        class TypeMap
        {
            public TypeMap(IList<PropertyExtractor<T>> properties)
            {
                Properties = properties;
            }

            public IList<PropertyExtractor<T>> Properties { get; }
        }
    }
}