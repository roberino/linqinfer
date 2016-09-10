using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Data
{
    public class BinaryVectorDocument : IBinaryPersistable
    {
        private const string PropertiesName = "PROP";
        private const string DataName = "DATA";
        private const string ChildrenName = "CLRN";

        private readonly IDictionary<string, string> _properties;
        private readonly IList<ColumnVector1D> _binData;
        private readonly IList<BinaryVectorDocument> _children;

        public BinaryVectorDocument()
        {
            _properties = new Dictionary<string, string>();
            _binData = new List<ColumnVector1D>();
            _children = new List<BinaryVectorDocument>();

            Version = 1;
            Timestamp = DateTime.UtcNow;
        }

        public int Version { get; set; }

        public DateTime Timestamp { get; private set; }

        public long Checksum
        {
            get
            {
                long checksum = 0;

                checksum ^= Version;

                foreach(var prop in _properties)
                {
                    checksum ^= prop.Key.GetHashCode() ^ prop.Value.GetHashCode();
                }

                foreach(var val in _binData)
                {
                    checksum ^= val.GetHashCode();
                }

                foreach (var cld in _children)
                {
                    checksum ^= cld.Checksum;
                }

                return checksum;
            }
        }

        private long AppendCheckSum(string val, long checksum)
        {
            foreach(var c in val)
            {
                checksum ^= c;
            }

            return checksum;
        }

        public IDictionary<string, string> Properties
        {
            get
            {
                return _properties;
            }
        }

        public T PropertyOrDefault<T>(string key, T defaultValue)
        {
            string val;

            if (_properties.TryGetValue(key, out val))
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }

            return defaultValue;
        }

        public IList<ColumnVector1D> Vectors
        {
            get
            {
                return _binData;
            }
        }

        public IList<BinaryVectorDocument> Children
        {
            get
            {
                return _children;
            }
        }
        public void Load(Stream input)
        {
            var reader = new BinaryReader(input, Encoding.UTF8);

            Read(reader, 0);
        }

        public void Save(Stream output)
        {
            var writer = new BinaryWriter(output, Encoding.UTF8);

            Write(writer, 0);
        }

        protected void Read(BinaryReader reader, int level)
        {
            var levelc = reader.ReadInt32();

            Version = reader.ReadInt32();

            var checksum = reader.ReadInt64();

            Timestamp = DateTime.FromBinary(reader.ReadInt64());

            ReadSection(PropertiesName, reader, (n, r) =>
            {
                _properties[r.ReadString()] = r.ReadString();
            });

            ReadSection(DataName, reader, (n, r) =>
            {
                var len = r.ReadInt32();
                var vect = ColumnVector1D.FromByteArray(r.ReadBytes(len));

                _binData.Add(vect);
            });

            ReadSection(ChildrenName, reader, (n, r) =>
            {
                var child = new BinaryVectorDocument();

                child.Read(r, level + 1);

                _children.Add(child);
            });

            if (Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
            }
        }

        private void ReadSection(string name, BinaryReader reader, Action<int, BinaryReader> action)
        {
            var currentName = reader.ReadString();
            var currentCount = reader.ReadInt32();

            foreach (var n in Enumerable.Range(0, currentCount))
            {
                action(n, reader);
            }
        }

        protected void Write(BinaryWriter writer, int level)
        {
            var date = DateTime.UtcNow;

            writer.Write(level);
            writer.Write(Version);
            writer.Write(Checksum);
            writer.Write(date.ToBinary());

            writer.Write(PropertiesName);
            writer.Write(_properties.Count);

            foreach(var prop in _properties)
            {
                writer.Write(prop.Key);
                writer.Write(prop.Value);
            }

            writer.Write(DataName);
            writer.Write(_binData.Count);

            foreach (var data in _binData)
            {
                var ba = data.ToByteArray();
                writer.Write(ba.Length);
                writer.Write(ba);
            }

            writer.Write(ChildrenName);
            writer.Write(_children.Count);

            foreach(var child in _children)
            {
                child.Write(writer, level + 1);
            }
        }
    }
}
