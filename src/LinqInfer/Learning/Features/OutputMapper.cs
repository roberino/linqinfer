using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class OutputMapper<T> : ICategoricalOutputMapper<T> where T : IEquatable<T>
    {
        OneHotEncoding<T> _encoder;

        public OutputMapper(IEnumerable<T> outputs): this (new OneHotEncoding<T>(new HashSet<T>(outputs)))
        {
        }

        OutputMapper(OneHotEncoding<T> encoder)
        {
            _encoder = encoder;
            FeatureMetadata = Feature.CreateDefaults(_encoder.Lookup.Keys.Select(k => k.ToString()));
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
                        ClassType = _encoder.Lookup.Single(x => x.Value == o.index).Key,
                        Score = o.value
                    };
                }
            }
        }

        public IEnumerable<T> OutputClasses => _encoder.Lookup.Keys;

        public int VectorSize => _encoder.Lookup.Count;

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