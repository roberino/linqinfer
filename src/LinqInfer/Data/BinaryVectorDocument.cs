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
        private const string BlobName = "BLOB";
        private const string DataName = "DATA";
        private const string ChildrenName = "CLRN";

        private readonly IDictionary<string, string> _properties;
        private readonly IDictionary<string, byte[]> _blobs;
        private readonly IList<ColumnVector1D> _vectorData;
        private readonly IList<BinaryVectorDocument> _children;

        public BinaryVectorDocument()
        {
            _properties = new Dictionary<string, string>();
            _blobs = new Dictionary<string, byte[]>();
            _vectorData = new List<ColumnVector1D>();
            _children = new List<BinaryVectorDocument>();

            Version = 1;
            Timestamp = DateTime.UtcNow;
        }

        public BinaryVectorDocument(Stream data) : this()
        {
            Load(data);
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

                foreach(var val in _vectorData)
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

        public IDictionary<string, byte[]> Blobs
        {
            get
            {
                return _blobs;
            }
        }

        public T PropertyOrDefault<T>(string key, T defaultValue)
        {
            string val;

            if (_properties.TryGetValue(key, out val))
            {
                if (typeof(T).IsEnum)
                {
                    try
                    {
                        return (T)Enum.Parse(typeof(T), val);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                return (T)Convert.ChangeType(val, typeof(T));
            }

            return defaultValue;
        }

        public IList<ColumnVector1D> Vectors
        {
            get
            {
                return _vectorData;
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

            var actions = new Dictionary<string, Action<int, BinaryReader>>();

            actions[PropertiesName] = (n, r) =>
            {
                _properties[r.ReadString()] = r.ReadString();
            };

            actions[DataName] = (n, r) =>
            {
                var len = r.ReadInt32();
                var vect = ColumnVector1D.FromByteArray(r.ReadBytes(len));

                _vectorData.Add(vect);
            };

            actions[BlobName] = (n, r) =>
            {
                var key = r.ReadString();
                var len = r.ReadInt32();
                var blob = r.ReadBytes(len);

                _blobs[key] = blob;
            };

            actions[ChildrenName] = (n, r) =>
            {
                var child = new BinaryVectorDocument();

                child.Read(r, level + 1);

                _children.Add(child);
            };

            ReadSections(reader, n => actions[n]);

            if (Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
            }
        }

        private void ReadSections(BinaryReader reader, Func<string, Action<int, BinaryReader>> readActionMapper)
        {
            while (true)
            {
                var currentName = reader.ReadString();
                var action = readActionMapper(currentName);
                var currentCount = reader.ReadInt32();

                foreach (var n in Enumerable.Range(0, currentCount))
                {
                    action(n, reader);
                }

                if (currentName == ChildrenName) break;
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
            writer.Write(_vectorData.Count);

            foreach (var data in _vectorData)
            {
                var ba = data.ToByteArray();
                writer.Write(ba.Length);
                writer.Write(ba);
            }

            writer.Write(BlobName);
            writer.Write(_blobs.Count);

            foreach(var blob in _blobs)
            {
                writer.Write(blob.Key);
                writer.Write(blob.Value.Length);
                writer.Write(blob.Value);
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
