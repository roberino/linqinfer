using System.IO;
using System.Xml.Linq;

namespace LinqInfer.Data.Serialisation
{
    public class XmlBlob<T> : IBinaryPersistable where T : IXmlExportable, IXmlImportable
    {
        internal XmlBlob(T instance)
        {
            Instance = instance;
        }

        public T Instance { get; }

        public void Load(Stream input)
        {
            var xml = XDocument.Load(input);

            Instance.ImportXml(xml);
        }

        public void Save(Stream output)
        {
            Instance.ExportAsXml().Save(output);
        }
    }
}