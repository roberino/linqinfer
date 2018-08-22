using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LinqInfer.Maths;
using LinqInfer.Utility;

namespace LinqInfer.Data.Serialisation
{
    /// <summary>
    /// General purpose document for serialising vector and general object data.
    /// The document supports serialising as XML and to a binary stream
    /// </summary>
    public class PortableDataDocument : IBinaryPersistable, IXmlExportable, IXmlImportable, IEquatable<PortableDataDocument>
    {
        const string PropertiesName = "Properties";
        const string BlobName = "Blob";
        const string DataName = "Data";
        const string ChildrenName = "Children";
        const string VectorName = "Vector";

        string _rootName;

        public PortableDataDocument()
        {
            var properties = new ConstrainableDictionary<string, string>(v => v != null);

            properties.AddContraint((k, v) => XmlConvert.VerifyName(k) != null);

            Properties = properties;
            Blobs = new Dictionary<string, byte[]>();
            Vectors = new List<IVector>();
            Children = new List<PortableDataDocument>();
            Timestamp = DateTime.UtcNow;
        }

        public PortableDataDocument(Stream data) : this()
        {
            Load(data);
        }

        public PortableDataDocument(XDocument xml, bool validate = false, XmlVectorSerialisationMode vectorToXmlSerialisationMode = XmlVectorSerialisationMode.Default) : this()
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

                foreach (var prop in Properties)
                {
                    checksum ^= prop.Key.GetHashCode() ^ prop.Value.GetHashCode();
                }

                foreach (var val in Vectors)
                {
                    checksum ^= val.GetHashCode();
                }

                foreach (var blob in Blobs)
                {
                    checksum ^= blob.Key.GetHashCode() ^ StructuralComparisons.StructuralEqualityComparer.GetHashCode(blob.Value);
                }

                foreach (var cld in Children)
                {
                    checksum ^= cld.Checksum;
                }

                return Math.Abs(checksum);
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

                _rootName = XmlConvert.VerifyName(n);
            }
            catch (XmlException) { }
        }

        internal string AssemblyQualifiedName => PropertyOrDefault(nameof(AssemblyQualifiedName), string.Empty);

        internal string TypeName => PropertyOrDefault(nameof(TypeName), string.Empty);

        public PortableDataDocument FindChild<T>()
        {
            return QueryChildren(new { typeof(T).AssemblyQualifiedName }).FirstOrDefault();
        }

        public IDictionary<string, string> Properties { get; }

        public IDictionary<string, byte[]> Blobs { get; }

        public IEnumerable<PortableDataDocument> QueryChildren(object propertyQuery)
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

        public IList<IVector> Vectors { get; }

        public IList<PortableDataDocument> Children { get; }

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

        internal PortableDataDocument GetChildDoc<T>(Type type = null, int? index = null, bool ignoreIfMissing = false)
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

            if (obj is IImportableFromDataDocument)
            {
                ((IImportableFromDataDocument)obj).ImportData(childNode);

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
                if (obj is IExportableAsDataDocument)
                {
                    var childDoc = ((IExportableAsDataDocument)obj).ExportData();

                    SetProperties(childDoc, attributes);

                    childDoc.SetType(childType);

                    Children.Add(childDoc);
                }
                else
                {
                    if (obj is IBinaryPersistable)
                    {
                        var childDoc = new PortableDataDocument();

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

            if (levelc != level) throw new ArgumentException("Invalid data");

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
                var child = new PortableDataDocument();

                child.Read(r, level + 1);

                Children.Add(child);
            };

            ReadSections(reader, n => actions[n]);

            if (ValidateOnImport && Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
            }
        }

        static void SetProperties(PortableDataDocument doc, object obj)
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

        XDocument ExportAsXml(bool isRoot, bool base64V)
        {
            var date = DateTime.UtcNow;

            var en = _rootName ?? (isRoot ? "Doc" : "Node");

            var doc = new XDocument(new XElement(en));

            if (isRoot)
            {
                doc.Root.SetAttributeValue("exported", date);
                doc.Root.SetAttributeValue("checksum", Checksum);
            }

            if (Properties.Any())
            {
                var propsNode = new XElement(PropertiesName,
                    Properties.Select(p => new XElement(p.Key, p.Value)));

                doc.Root.Add(propsNode);
            }

            if (Vectors.Any())
            {
                var dataNode = new XElement(DataName,
                    Vectors.Select(v => new XElement(VectorName,
                    VectorSerialiser.Serialize(v, base64V))));
                doc.Root.Add(dataNode);
            }

            if (Blobs.Any())
            {
                var blobsNode = new XElement(BlobName + "s",
                    Blobs.Select(b => new XElement(BlobName,
                        new XAttribute("key", b.Key), Convert.ToBase64String(b.Value))));
                doc.Root.Add(blobsNode);
            }

            if (Children.Any())
            {
                var childrenNode = new XElement(ChildrenName, Children.Select(c => c.ExportAsXml(false, base64V).Root));

                doc.Root.Add(childrenNode);
            }

            return doc;
        }

        long AppendCheckSum(string val, long checksum)
        {
            foreach (var c in val)
            {
                checksum ^= c;
            }

            return checksum;
        }

        void ReadSections(BinaryReader reader, Func<string, Action<int, BinaryReader>> readActionMapper)
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

        void ImportXml(XElement rootNode)
        {
            var checksum = int.Parse(rootNode.Attribute("checksum")?.Value ?? "0");

            foreach (var prop in ChildElements(rootNode, PropertiesName))
            {
                Properties[prop.Name.LocalName] = prop.Value;
            }

            foreach (var vect in ChildElements(rootNode, DataName).Where(e => e.Name.LocalName == VectorName))
            {
                var vd = VectorSerialiser.Deserialize(vect.Value,
                    VectorToXmlSerialisationMode == XmlVectorSerialisationMode.Base64);

                Vectors.Add(vd);
            }

            foreach (var blob in ChildElements(rootNode, BlobName + "s").Where(e => e.Name.LocalName == BlobName))
            {
                Blobs[blob.Attribute("key").Value] = Convert.FromBase64String(blob.Value.Trim());
            }

            foreach (var child in ChildElements(rootNode, ChildrenName))
            {
                var cdoc = new PortableDataDocument()
                {
                    ValidateOnImport = ValidateOnImport,
                    VectorToXmlSerialisationMode = VectorToXmlSerialisationMode
                };

                cdoc.ImportXml(child);

                Children.Add(cdoc);
            }

            if (ValidateOnImport && Checksum != checksum)
            {
                throw new ArgumentException("Invalid or corrupted data");
            }
        }

        IEnumerable<XElement> ChildElements(XElement parent, string name)
        {
            var e = parent.Element(XName.Get(name));

            return (e?.Elements() ?? Enumerable.Empty<XElement>());
        }

        protected void Write(BinaryWriter writer, int level)
        {
            var date = DateTime.UtcNow;

            writer.Write(level);
            writer.Write(Checksum);
            writer.Write(date.ToBinary());

            writer.Write(PropertiesName);
            writer.Write(Properties.Count);

            foreach (var prop in Properties)
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

            foreach (var blob in Blobs)
            {
                writer.Write(blob.Key);
                writer.Write(blob.Value.Length);
                writer.Write(blob.Value);
            }

            writer.Write(ChildrenName);
            writer.Write(Children.Count);

            foreach (var child in Children)
            {
                child.Write(writer, level + 1);
            }
        }

        public bool Equals(PortableDataDocument other)
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
            return Equals(obj as PortableDataDocument);
        }

        public override int GetHashCode()
        {
            return ExportAsXml().ToString().GetHashCode();
        }
    }
}