using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LinqInfer.Data
{
    /// <summary>
    /// General purpose document for serialising vector and general object data.
    /// The document supports serialising as XML and to a binary stream
    /// </summary>
    public class BinaryVectorDocument : IBinaryPersistable, IXmlExportable, IXmlImportable, IEquatable<BinaryVectorDocument>
    {
        private const string PropertiesName = "PROP";
        private const string BlobName = "BLOB";
        private const string DataName = "DATA";
        private const string ChildrenName = "CLRN";

        private string _rootName;

        public BinaryVectorDocument()
        {
            Properties = new ConstrainableDictionary<string, string>(v => v != null);
            Blobs = new Dictionary<string, byte[]>();
            Vectors = new List<IVector>();
            Children = new List<BinaryVectorDocument>();

            Version = 1;
            Timestamp = DateTime.UtcNow;
        }

        public BinaryVectorDocument(Stream data) : this()
        {
            Load(data);
        }

        public BinaryVectorDocument(XDocument xml, bool validate = false, XmlVectorSerialisationMode vectorToXmlSerialisationMode = XmlVectorSerialisationMode.Default) : this()
        {
            VectorToXmlSerialisationMode = vectorToXmlSerialisationMode;
            ValidateOnImport = validate;
            ImportXml(xml);
        }

        /// <summary>
        /// If true, the checksum will be validated on import
        /// </summary>
        public bool ValidateOnImport { get; set; } = true;

        public IVectorSerialiser VectorSerialiser { get; set; } = new VectorSerialiser();

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

                foreach(var prop in Properties)
                {
                    checksum ^= prop.Key.GetHashCode() ^ prop.Value.GetHashCode();
                }

                foreach(var val in Vectors)
                {
                    checksum ^= val.GetHashCode();
                }

                foreach (var cld in Children)
                {
                    checksum ^= cld.Checksum;
                }

                return checksum;
            }
        }

        public void SetType<T>(T instance = default(T))
        {
            var type = instance?.GetType() ?? typeof(T);

            SetType(type);
        }

        internal void SetType(Type type)
        {
            Properties[nameof(AssemblyQualifiedName)] = type.AssemblyQualifiedName;
            Properties[nameof(TypeName)] = type.Name;

            try
            {
                var n = type.Name;
                var i = n.IndexOf('`');

                if (i > -1)
                {
                    n = n.Substring(0, i);
                }

                _rootName = XmlConvert.VerifyName(n)?.ToLowerInvariant();
            }
            catch (XmlException) { }
        }

        internal string AssemblyQualifiedName => PropertyOrDefault(nameof(AssemblyQualifiedName), string.Empty);

        internal string TypeName => PropertyOrDefault(nameof(TypeName), string.Empty);

        public BinaryVectorDocument FindChild<T>()
        {
            return QueryChildren(new { typeof(T).AssemblyQualifiedName }).FirstOrDefault();
        }

        public IDictionary<string, string> Properties { get;  }

        public IDictionary<string, byte[]> Blobs { get; }

        public IEnumerable<BinaryVectorDocument> QueryChildren(object propertyQuery)
        {
            var query = propertyQuery.ToDictionary();

            return Children.Where(c =>
            {
                bool found = true;

                foreach (var q in query)
                {
                    if (c.Properties.TryGetValue(q.Key, out string v) && v == q.Value?.ToString())
                    {
                        continue;
                    }

                    found = false;
                    break;
                }

                return found;
            });
        }

        public bool HasProperty(string name)
        {
            return Properties.ContainsKey(name);
        }

        public T PropertyOrDefault<T>(string key, T defaultValue)
        {
            string val;

            if (Properties.TryGetValue(key, out val))
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

        public IList<IVector> Vectors { get; private set; }

        public BinaryVectorDocument AddChild()
        {
            var doc = new BinaryVectorDocument();
            Children.Add(doc);
            return doc;
        }

        public IList<BinaryVectorDocument> Children { get; private set; }

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
            return ExportAsXml(true, VectorToXmlSerialisationMode == XmlVectorSerialisationMode.Base64);
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

        internal T PropertyOrDefault<T>(Expression<Func<object>> keyExpression, T defaultValue)
        {
            var propName = LinqExtensions.GetPropertyName(keyExpression);

            return PropertyOrDefault(propName, defaultValue);
        }

        internal void SetPropertyFromExpression(Expression<Func<object>> expression, object value = null)
        {
            var propName = LinqExtensions.GetPropertyName(expression);

            if (value == null)
            {
                value = expression.Compile().Invoke();

                if (value != null)
                {
                    Properties[propName] = value.ToString();
                }
            }
            else
            {
                Properties[propName] = value.ToString();
            }
        }

        internal BinaryVectorDocument GetChildDoc<T>(Type type = null, int? index = null, bool ignoreIfMissing = false)
        {
            var tname = (type ?? typeof(T)).GetTypeInf().Name;

            var childNode = index.HasValue ? Children[index.Value] : Children.SingleOrDefault(c => c.TypeName == tname);

            if (childNode == null && !ignoreIfMissing) throw new FormatException("Child object not found : " + tname);

            return childNode;
        }

        internal T ReadChildObject<T>(T obj, int? index = null, bool ignoreIfMissing = false)
        {
            if (obj == null) return obj;

            var childNode = GetChildDoc<T>(obj.GetType(), index, ignoreIfMissing);

            if (childNode == null) return obj;

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

        internal void WriteChildObject(object obj, object attributes = null)
        {
            if (obj == null) return;

            var childType = obj.GetType();
            var tc = Type.GetTypeCode(childType);

            if (tc == TypeCode.Object)
            {
                if (obj is IExportableAsVectorDocument)
                {
                    var childDoc = ((IExportableAsVectorDocument)obj).ToVectorDocument();

                    SetProperties(childDoc, attributes);

                    childDoc.SetType(childType);
                    
                    Children.Add(childDoc);
                }
                else
                {
                    if (obj is IBinaryPersistable)
                    {
                        var childDoc = new BinaryVectorDocument();

                        SetProperties(childDoc, attributes);
                        childDoc.SetType(childType);                        
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
                Properties[r.ReadString()] = r.ReadString();
            };

            actions[DataName] = (n, r) =>
            {
                var len = r.ReadInt32();
                var vect = ColumnVector1D.FromByteArray(r.ReadBytes(len));

                Vectors.Add(vect);
            };

            actions[BlobName] = (n, r) =>
            {
                var key = r.ReadString();
                var len = r.ReadInt32();
                var blob = r.ReadBytes(len);

                Blobs[key] = blob;
            };

            actions[ChildrenName] = (n, r) =>
            {
                var child = new BinaryVectorDocument();

                child.Read(r, level + 1);

                Children.Add(child);
            };

            ReadSections(reader, n => actions[n]);

            if (ValidateOnImport && Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
            }
        }

        private void SetProperties(BinaryVectorDocument doc, object obj)
        {
            if (obj != null)
            {
                var data = obj.ToDictionary();

                foreach (var item in data)
                {
                    doc.Properties[item.Key] = item.Value.ToString();
                }
            }
        }

        private XDocument ExportAsXml(bool isRoot, bool base64v)
        {
            var date = DateTime.UtcNow;

            var en = _rootName ?? (isRoot ? "doc" : "node");

            var doc = new XDocument(new XElement(en,
                new XAttribute("version", Version),
                new XAttribute("checksum", Checksum)));

            if (isRoot) doc.Root.Add(new XAttribute("exported", date));

            if (Properties.Any())
            {
                var propsNode = new XElement(PropertiesName.ToLower(),
                    Properties.Select(p => new XElement("property",
                        new XAttribute("key", p.Key),
                        new XAttribute("value", p.Value))));

                doc.Root.Add(propsNode);
            }

            if (Vectors.Any())
            {
                var dataNode = new XElement(DataName.ToLower(),
                    Vectors.Select(v => new XElement("vector", 
                    VectorSerialiser.Serialize(v, base64v))));
                doc.Root.Add(dataNode);
            }

            if (Blobs.Any())
            {
                var blobsNode = new XElement(BlobName.ToLower() + "s",
                    Blobs.Select(b => new XElement(BlobName.ToLower(),
                        new XAttribute("key", b.Key), Convert.ToBase64String(b.Value))));
                doc.Root.Add(blobsNode);
            }

            if (Children.Any())
            {
                var childrenNode = new XElement(ChildrenName.ToLower(), Children.Select(c => c.ExportAsXml(false, base64v).Root));

                doc.Root.Add(childrenNode);
            }

            return doc;
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

            foreach (var prop in ChildElements(rootNode, PropertiesName).Where(e => e.Name.LocalName == "property" && e.HasAttributes))
            {
                Properties[prop.Attribute("key").Value] = prop.Attribute("value").Value;
            }

            foreach (var vect in ChildElements(rootNode, DataName).Where(e => e.Name.LocalName == "vector"))
            {
                var vd = VectorSerialiser.Deserialize(vect.Value, VectorToXmlSerialisationMode == XmlVectorSerialisationMode.Base64);

                Vectors.Add(vd);
            }
            
            foreach (var blob in ChildElements(rootNode, BlobName + "s").Where(e => e.Name.LocalName == BlobName.ToLower()))
            {
                Blobs[blob.Attribute("key").Value] = Convert.FromBase64String(blob.Value.Trim());
            }

            foreach (var child in ChildElements(rootNode, ChildrenName))
            {
                var cdoc = new BinaryVectorDocument() { ValidateOnImport = ValidateOnImport, VectorToXmlSerialisationMode = VectorToXmlSerialisationMode };

                cdoc.ImportXml(child);

                Children.Add(cdoc);
            }

            if (ValidateOnImport && Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
            }
        }

        private IEnumerable<XElement> ChildElements(XElement parent, string name)
        {
            var e = parent.Element(XName.Get(name.ToLower()));

            return (e?.Elements() ?? Enumerable.Empty<XElement>());
        }

        protected void Write(BinaryWriter writer, int level)
        {
            var date = DateTime.UtcNow;

            writer.Write(level);
            writer.Write(Version);
            writer.Write(Checksum);
            writer.Write(date.ToBinary());

            writer.Write(PropertiesName);
            writer.Write(Properties.Count);

            foreach(var prop in Properties)
            {
                writer.Write(prop.Key);
                writer.Write(prop.Value);
            }

            writer.Write(DataName);
            writer.Write(Vectors.Count);

            foreach (var data in Vectors)
            {
                var ba = data.ToByteArray();
                writer.Write(ba.Length);
                writer.Write(ba);
            }

            writer.Write(BlobName);
            writer.Write(Blobs.Count);

            foreach(var blob in Blobs)
            {
                writer.Write(blob.Key);
                writer.Write(blob.Value.Length);
                writer.Write(blob.Value);
            }

            writer.Write(ChildrenName);
            writer.Write(Children.Count);

            foreach(var child in Children)
            {
                child.Write(writer, level + 1);
            }
        }

        public bool Equals(BinaryVectorDocument other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Checksum != other.Checksum) return false;

            var xml1 = ExportAsXml();
            var xml2 = other.ExportAsXml();

            xml1.Root.Attribute("exported").Remove();
            xml2.Root.Attribute("exported").Remove();

            return string.Equals(xml1.ToString(), xml2.ToString());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BinaryVectorDocument);
        }

        public override int GetHashCode()
        {
            return ExportAsXml().ToString().GetHashCode();
        }
    }
}
