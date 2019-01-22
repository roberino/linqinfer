using System;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Features
{
    class OneHotEncoding<T> : IOneHotEncoding<T>
    {
        public OneHotEncoding(int maxSize)
        {
            ArgAssert.AssertGreaterThanZero(maxSize, nameof(maxSize));

            Lookup = new Dictionary<T, int>();
            VectorSize = maxSize;
        }

        public OneHotEncoding(ISet<T> classes)
        {
            int i = 0;
            Lookup = classes.ToDictionary(c => c, _ => i++);
            VectorSize = Lookup.Count;

            ArgAssert.AssertGreaterThanZero(VectorSize, nameof(classes));
        }

        OneHotEncoding(IDictionary<T, int> lookup, int? maxVectorSize = null)
        {
            Lookup = lookup;
            VectorSize = maxVectorSize.GetValueOrDefault(Lookup.Count);
        }

        public int VectorSize { get; }

        public OneOfNVector Encode(T obj)
        {
            return new OneOfNVector(VectorSize, GetIndex(obj));
        }

        public BitVector Encode(IEnumerable<T> categories)
        {
            var indexes = new List<int>();

            foreach (var cat in categories)
            {
                var index = GetIndex(cat);

                if (index.HasValue)
                    indexes.Add(index.Value);
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

            doc.SetPropertyFromExpression(() => VectorSize);

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
                doc.Properties["_" + output.Key] = output.Value.ToString();
            }

            return doc;
        }

        public IEnumerable<KeyValuePair<T, int>> IndexTable => Lookup.Select(x => new KeyValuePair<T, int>(x.Key, x.Value));

        public static OneHotEncoding<T> ImportData(PortableDataDocument data)
        {
            var vectorSize = data.PropertyOrDefault(nameof(VectorSize), 0);

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

            foreach (var item in data.Properties.Where(k => k.Key.StartsWith("_")))
            {
                outputs[(T) Convert.ChangeType(item.Key.Substring(1), type)] = int.Parse(item.Value);
            }

            return new OneHotEncoding<T>(outputs, vectorSize > 0 ? vectorSize : new int?());
        }

        int? GetIndex(T obj)
        {
            if (obj == null)
            {
                return new int?();
            }

            if (Lookup.TryGetValue(obj, out var index))
            {
                return index;
            }

            if (Lookup.Count < VectorSize)
            {
                index = Lookup.Count;

                Lookup[obj] = index;

                return index;
            }

            return new int?();
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