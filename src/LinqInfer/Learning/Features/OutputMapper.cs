using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class OutputMapper<T> : ICategoricalOutputMapper<T> where T : IEquatable<T>
    {
        IDictionary<T, int> _outputs;

        public OutputMapper()
        {
        }

        public OutputMapper(Stream input)
        {
            Load(input);
        }

        public OutputMapper(IEnumerable<T> outputs)
        {
            Initialise(outputs);
        }

        public OutputMapper(IDictionary<T, int> outputs)
        {
            _outputs = outputs;
        }

        public void Initialise(IEnumerable<T> outputs)
        {
            var i = 0;
            _outputs = outputs.ToDictionary(o => o, o => i++);
            IndexLookup = _outputs.ToDictionary(o => o.Key.ToString(), o => o.Value);
            FeatureMetadata = Feature.CreateDefaults(IndexLookup.Keys);
        }

        public IEnumerable<ClassifyResult<T>> Map(IVector output)
        {
            var i = 0;
            var indexes = output.ToColumnVector().Select(o => new { value = o, index = i++ }).ToArray().OrderByDescending(o => o.value);

            foreach (var o in indexes)
            {
                if (o.value > 0)
                {
                    yield return new ClassifyResult<T>()
                    {
                        ClassType = _outputs.Single(x => x.Value == o.index).Key,
                        Score = o.value
                    };
                }
            }
        }

        public IEnumerable<T> OutputClasses => _outputs.Keys;

        public IDictionary<string, int> IndexLookup { get; private set; }

        public int VectorSize => _outputs?.Count ?? 0;

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public virtual double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public IVector ExtractIVector(T obj)
        {
            if (_outputs == null) throw new InvalidOperationException();
            
            return new OneOfNVector(VectorSize, _outputs[obj]);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();
            var tc = Type.GetTypeCode(typeof(T));

            if (tc == TypeCode.Object)
            {
                using (var ms = new MemoryStream())
                {
                    Save(ms);

                    doc.Blobs.Add(nameof(OutputClasses), ms.ToArray());
                }

                return doc;
            }

            foreach(var output in _outputs)
            {
                doc.Properties[output.Key.ToString()] = output.Value.ToString();
            }

            return doc;
        }

        public static OutputMapper<T> ImportData(PortableDataDocument data)
        {
            var type = typeof(T);
            var tc = Type.GetTypeCode(type);

            if (tc == TypeCode.Object)
            {
                using (var ms = new MemoryStream(data.Blobs[nameof(OutputClasses)]))
                {
                    return new OutputMapper<T>(ms);
                }
            }

            var outputs = new Dictionary<T, int>();

            foreach (var item in data.Properties)
            {
                outputs[(T) Convert.ChangeType(item.Key, type)] = int.Parse(item.Value);
            }

            return new OutputMapper<T>(outputs);
        }

        void Save(Stream stream)
        {
            var sz = new DictionarySerialiser<T, int>();

            sz.Write(_outputs, stream);
        }

        void Load(Stream stream)
        {
            var sz = new DictionarySerialiser<T, int>();

            _outputs = sz.Read(stream);
        }
    }
}