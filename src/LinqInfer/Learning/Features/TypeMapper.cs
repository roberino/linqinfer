using LinqInfer.Data.Serialisation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class TypeMapper<T> : IExportableAsDataDocument
    {
        readonly object _lockObj = new object();
        readonly Dictionary<Type, TypeMap> _typeMaps;
        readonly Dictionary<string, int> _indexMap;
        readonly int _maxVectorSize;

        int _maxIndex;

        public TypeMapper(int maxVectorSize)
        {
            _typeMaps = new Dictionary<Type, TypeMap>();
            _indexMap = new Dictionary<string, int>();
            _maxIndex = -1;
            _maxVectorSize = maxVectorSize;

            VectorSize = maxVectorSize;
        }

        public int CurrentSize => _maxIndex + 1;

        public int VectorSize { get; private set; }
        
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

        public void RegisterInstance(T obj)
        {
            GetOrCreateMap(obj.GetType());
        }

        public void Resize()
        {
            VectorSize = _maxIndex + 1;
        }

        protected TypeMap GetOrCreateMap(Type type)
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
                if (_maxIndex >= _maxVectorSize - 1)
                {
                    return null;
                }

                index = ++_maxIndex;

                _indexMap[name] = index;
            }

            return index;
        }

        protected class TypeMap
        {
            public TypeMap(IList<PropertyExtractor<T>> properties)
            {
                Properties = properties;
            }

            public IList<PropertyExtractor<T>> Properties { get; }
        }
    }
}