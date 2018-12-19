using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class OutputMapper<T> : ICategoricalOutputMapper<T> where T : IEquatable<T>
    {
        IOneHotEncoding<T> _encoder;

        public OutputMapper(IEnumerable<T> outputs): this (new OneHotEncoding<T>(new HashSet<T>(outputs)))
        {
        }

        internal OutputMapper(IOneHotEncoding<T> encoder)
        {
            _encoder = encoder;
            FeatureMetadata = Feature.CreateDefaults(_encoder.IndexTable.Select(k => k.ToString()));
        }

        public IEnumerable<ClassifyResult<T>> Map(IVector output)
        {
            var i = 0;

            var indexes = output.ToColumnVector()
                .Select(o => new {value = o, index = i++}).ToArray()
                .OrderByDescending(o => o.value);

            foreach (var o in indexes)
            {
                if (o.value > 0)
                {
                    yield return new ClassifyResult<T>()
                    {
                        ClassType = _encoder.IndexTable.Single(x => x.Value == o.index).Key,
                        Score = o.value
                    };
                }
            }
        }

        public IEnumerable<T> OutputClasses => _encoder.IndexTable.Select(x => x.Key);

        public int VectorSize => _encoder.IndexTable.Count();

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public IVector ExtractIVector(T obj)
        {
            return _encoder.Encode(obj);
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public PortableDataDocument ExportData()
        {
            var doc = _encoder.ExportData();

            doc.SetType(this);

            return doc;
        }

        public static OutputMapper<T> ImportData(PortableDataDocument data)
        {
            var encoder = OneHotEncoding<T>.ImportData(data);

            return new OutputMapper<T>(encoder);
        }
    }
}