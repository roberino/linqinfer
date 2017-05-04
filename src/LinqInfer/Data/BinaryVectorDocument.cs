﻿using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LinqInfer.Data
{
    /// <summary>
    /// General purpose document for serialising vector and general object data.
    /// The document supports serialising as XML and to a binary stream
    /// </summary>
    public class BinaryVectorDocument : IBinaryPersistable, IXmlExportable, IXmlImportable
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
            _properties = new ConstrainableDictionary<string, string>(v => v != null);
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

        public BinaryVectorDocument(XDocument xml, bool validate = false) : this()
        {
            ValidateOnImport = validate;
            ImportXml(xml);
        }

        /// <summary>
        /// If true, the checksum will be validated on import
        /// </summary>
        public bool ValidateOnImport { get; set; } = true;

        public XmlVectorSerialisationMode VectorToXmlSerialisationMode { get; set; } = XmlVectorSerialisationMode.Default;

        /// <summary>
        /// A version number assigned to the document
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The time of creation
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// A basic checksum
        /// </summary>
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

        public bool HasProperty(string name)
        {
            return _properties.ContainsKey(name);
        }

        public T PropertyOrDefault<T>(string key, T defaultValue)
        {
            string val;

            if (_properties.TryGetValue(key, out val))
            {
                if (typeof(T).GetTypeInf().IsEnum)
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

        public XDocument ExportAsXml()
        {
            var date = DateTime.UtcNow;
            var base64v = VectorToXmlSerialisationMode == XmlVectorSerialisationMode.Base64;

            var doc = new XDocument(new XElement("doc",
                new XAttribute("version", Version),
                new XAttribute("checksum", Checksum),
                new XAttribute("exported", date)));

            var propsNode = new XElement(PropertiesName.ToLower(),
                _properties.Select(p => new XElement("property",
                    new XAttribute("key", p.Key),
                    new XAttribute("value", p.Value))));

            var dataNode = new XElement(DataName.ToLower(),
                _vectorData.Select(v => new XElement("vector", base64v ? v.ToBase64() : v.ToCsv(',', int.MaxValue))));

            var blobsNode = new XElement(BlobName.ToLower() + "s",
                _blobs.Select(b => new XElement(BlobName.ToLower(),
                    new XAttribute("key", b.Key), Convert.ToBase64String(b.Value))));

            var childrenNode = new XElement(ChildrenName.ToLower(), _children.Select(c => c.ExportAsXml().Root));

            doc.Root.Add(propsNode);
            doc.Root.Add(dataNode);
            doc.Root.Add(blobsNode);
            doc.Root.Add(childrenNode);

            return doc;
        }

        public void ImportXml(XDocument xml)
        {
            ImportXml(xml.Root);
        }

        public enum XmlVectorSerialisationMode
        {
            Default,
            Base64,
            Csv
        }

        internal BinaryVectorDocument GetChildDoc<T>(Type type = null, int? index = null)
        {
            var tname = (type ?? typeof(T)).FullName;

            var childNode = index.HasValue ? Children[index.Value] : Children.SingleOrDefault(c => c.HasProperty("TypeName") && c.Properties["TypeName"] == tname);

            if (childNode == null) throw new NullReferenceException("Child object not found : " + tname);

            return childNode;
        }

        internal T ReadChildObject<T>(T obj, int? index = null)
        {
            if (obj == null) return obj;

            var childNode = GetChildDoc<T>(obj.GetType(), index);

            if (obj is IImportableAsVectorDocument)
            {
                ((IImportableAsVectorDocument)obj).FromVectorDocument(childNode);

                return obj;
            }

            if (obj is IBinaryPersistable)
            {
                ((IBinaryPersistable)obj).FromClob(childNode.Properties["Data"]);

                return obj;
            }

            throw new NotSupportedException();
        }

        internal void WriteChildObject(object obj)
        {
            if (obj == null) return;

            var tc = Type.GetTypeCode(obj.GetType());

            if (tc == TypeCode.Object)
            {
                if (obj is IExportableAsVectorDocument && obj is IImportableAsVectorDocument)
                {
                    var cdoc = ((IExportableAsVectorDocument)obj).ToVectorDocument();

                    cdoc.Properties["TypeName"] = obj.GetType().FullName;
                    cdoc.Properties["TypeGuid"] = obj.GetType().GUID.ToString();

                    Children.Add(cdoc);
                }
                else
                {
                    if (obj is IBinaryPersistable)
                    {
                        var childDoc = new BinaryVectorDocument();

                        childDoc.Properties["TypeName"] = obj.GetType().FullName;
                        childDoc.Properties["Data"] = ((IBinaryPersistable)obj).ToClob();

                        Children.Add(childDoc);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        protected void Read(BinaryReader reader, int level)
        {
            var levelc = reader.ReadInt32();

			if(levelc != level) throw new ArgumentException("Invalid data");

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

            if (ValidateOnImport && Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
            }
        }

        private long AppendCheckSum(string val, long checksum)
        {
            foreach (var c in val)
            {
                checksum ^= c;
            }

            return checksum;
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

        private void ImportXml(XElement rootNode)
        {
            var checksum = int.Parse(rootNode.Attribute("checksum").Value);

            Version = int.Parse(rootNode.Attribute("version").Value);

            var tfa = rootNode.Attribute("typeFactory");

            if (tfa != null && !string.IsNullOrEmpty(tfa.Value))
            {
                // TypeFactory = Type.GetType(tfa.Value);
            }

            foreach (var prop in rootNode.Element(XName.Get(PropertiesName.ToLower())).Elements().Where(e => e.Name.LocalName == "property" && e.HasAttributes))
            {
                _properties[prop.Attribute("key").Value] = prop.Attribute("value").Value;
            }

            foreach (var vect in rootNode.Element(XName.Get(DataName.ToLower())).Elements().Where(e => e.Name.LocalName == "vector"))
            {
                var vd = new ColumnVector1D(VectorToXmlSerialisationMode == XmlVectorSerialisationMode.Base64 ? Vector.FromBase64(vect.Value.Trim()) : Vector.FromCsv(vect.Value));

                _vectorData.Add(vd);
            }

            foreach (var blob in rootNode.Element(XName.Get(BlobName.ToLower() + "s")).Elements().Where(e => e.Name.LocalName == BlobName.ToLower()))
            {
                _blobs[blob.Attribute("key").Value] = Convert.FromBase64String(blob.Value.Trim());
            }

            foreach (var child in rootNode.Element(XName.Get(ChildrenName.ToLower())).Elements())
            {
                var cdoc = new BinaryVectorDocument() { ValidateOnImport = ValidateOnImport, VectorToXmlSerialisationMode = VectorToXmlSerialisationMode };

                cdoc.ImportXml(child);

                _children.Add(cdoc);
            }

            if (ValidateOnImport && Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
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
