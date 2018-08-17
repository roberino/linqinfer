using System;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Features
{
    class OneHotEncoding<T> : IExportableAsDataDocument
    {
        public OneHotEncoding(ISet<T> classes)
        {
            int i = 0;
            Lookup = classes.ToDictionary(c => c, _ => i++);
        }

        OneHotEncoding(IDictionary<T, int> lookup)
        {
            Lookup = lookup;
        }

        public int VectorSize => Lookup.Count;

        public OneOfNVector Encode(T obj)
        {
            if (Lookup.TryGetValue(obj, out int index))
            {
                return new OneOfNVector(VectorSize, index);
            }

            return new OneOfNVector(VectorSize);
        }

        public BitVector Encode(IEnumerable<T> categories)
        {
            var indexes = new List<int>();

            foreach (var cat in categories)
            {
                if (Lookup.TryGetValue(cat, out int index))
                {
                    indexes.Add(index);
                }
            }

            return new BitVector(indexes, VectorSize);
        }

        public IVector Encode(T[] categories)
        {
            if (categories.Length == 0) return new ZeroVector(VectorSize);
            if (categories.Length == 1) return Encode(categories[0]);
            return Encode(categories as IEnumerable<T>);
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

                    doc.Blobs.Add(nameof(Lookup), ms.ToArray());
                }

                return doc;
            }

            foreach (var output in Lookup)
            {
                doc.Properties[output.Key.ToString()] = output.Value.ToString();
            }

            return doc;
        }

        public static OneHotEncoding<T> ImportData(PortableDataDocument data)
        {
            var type = typeof(T);
            var tc = Type.GetTypeCode(type);

            if (tc == TypeCode.Object)
            {
                using (var ms = new MemoryStream(data.Blobs[nameof(Lookup)]))
                {
                    return new OneHotEncoding<T>(Load(ms));
                }
            }

            var outputs = new Dictionary<T, int>();

            foreach (var item in data.Properties)
            {
                outputs[(T) Convert.ChangeType(item.Key, type)] = int.Parse(item.Value);
            }

            return new OneHotEncoding<T>(outputs);
        }

        void Save(Stream stream)
        {
            var sz = new DictionarySerialiser<T, int>();

            sz.Write(Lookup, stream);
        }

        static IDictionary<T, int> Load(Stream stream)
        {
            var sz = new DictionarySerialiser<T, int>();

            return sz.Read(stream);
        }

        internal IDictionary<T, int> Lookup { get; }
    }
}