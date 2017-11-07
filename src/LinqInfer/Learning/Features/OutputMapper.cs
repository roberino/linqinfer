using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class OutputMapper<T> : ICategoricalOutputMapper<T> where T : IEquatable<T>
    {
        private IDictionary<T, int> _outputs;

        public OutputMapper()
        {
        }

        public OutputMapper(Stream input)
        {
            Load(input);
        }

        public OutputMapper(IEnumerable<T> outputs = null)
        {
            if (outputs != null) Initialise(outputs);
        }

        public virtual void Initialise(IEnumerable<T> outputs)
        {
            int i = 0;
            _outputs = outputs.ToDictionary(o => o, o => i++);
            IndexLookup = _outputs.ToDictionary(o => o.Key.ToString(), o => o.Value);
            FeatureMetadata = Feature.CreateDefaults(IndexLookup.Keys);
        }

        public virtual IEnumerable<ClassifyResult<T>> Map(IVector output)
        {
            int i = 0;
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

        public IEnumerable<T> OutputClasses
        {
            get
            {
                return _outputs.Keys;
            }
        }

        public IDictionary<string, int> IndexLookup { get; private set; }

        public int VectorSize { get { return _outputs == null ? 0 : _outputs.Count; } }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public virtual double[] ExtractVector(T obj)
        {
            return ExtractColumnVector(obj).GetUnderlyingArray();
        }

        public virtual ColumnVector1D ExtractColumnVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector();
        }

        public IVector ExtractIVector(T obj)
        {
            if (_outputs == null) throw new InvalidOperationException();

            return new OneOfNVector(VectorSize, _outputs[obj]);
        }

        public virtual void Save(Stream stream)
        {
            var sz = new DictionarySerialiser<T, int>();

            sz.Write(_outputs, stream);
        }

        public virtual void Load(Stream stream)
        {
            var sz = new DictionarySerialiser<T, int>();

            _outputs = sz.Read(stream);
        }
    }
}