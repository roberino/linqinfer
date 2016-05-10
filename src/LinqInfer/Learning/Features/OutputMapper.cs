using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class OutputMapper<T> : IOutputMapper<T> where T : IEquatable<T>
    {
        private IDictionary<T, int> _outputs;

        public OutputMapper(IEnumerable<T> outputs = null)
        {
            if (outputs != null) Initialise(outputs);
        }

        public virtual void Initialise(IEnumerable<T> outputs)
        {
            int i = 0;
            _outputs = outputs.ToDictionary(o => o, o => i++);
            IndexLookup = _outputs.ToDictionary(o => o.Key.ToString(), o => o.Value);
            FeatureMetadata = Feature.CreateDefault(IndexLookup.Keys);
        }

        public virtual IEnumerable<ClassifyResult<T>> Map(ColumnVector1D output)
        {
            int i = 0;
            var indexes = output.Select(o => new { value = o, index = i++ }).ToArray().OrderByDescending(o => o.value);

            foreach(var o in indexes)
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

        public IDictionary<string, int> IndexLookup { get; private set; }

        public int VectorSize { get { return _outputs == null ? 0 : _outputs.Count; } }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public virtual double[] CreateNormalisingVector(T sample = default(T))
        {
            if (_outputs == null) throw new InvalidOperationException();

            return _outputs.Select(o => 1d).ToArray();
        }

        public double[] NormaliseUsing(IEnumerable<T> samples)
        {
            return CreateNormalisingVector();
        }

        public virtual double[] ExtractVector(T obj)
        {
            if (_outputs == null) throw new InvalidOperationException();

            var o = _outputs[obj];

            var vector = Enumerable.Range(0, _outputs.Count).Select(n => 0d).ToArray();

            vector[o] = 1;

            return vector;
        }
    }
}