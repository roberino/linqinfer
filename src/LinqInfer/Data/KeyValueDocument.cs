using LinqInfer.Data.Serialisation;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Data
{
    class KeyValueDocument : IExportableAsDataDocument, IImportableFromDataDocument
    {
        readonly IDictionary<string, string> _keyValues;

        public KeyValueDocument() : this(new Dictionary<string, string>())
        {
        }

        public KeyValueDocument(IDictionary<string, string> keyValues)
        {
            _keyValues = keyValues;
        }

        public IDictionary<string, string> Data => _keyValues;

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            foreach (var kv in _keyValues)
            {
                doc.Properties[kv.Key] = kv.Value;
            }

            return doc;
        }

        public void ImportData(PortableDataDocument doc)
        {
            _keyValues.Clear();

            foreach (var prop in doc.Properties.Where(p => !p.Key.StartsWith("_")))
            {
                _keyValues[prop.Key] = prop.Value;
            }
        }
    }
}