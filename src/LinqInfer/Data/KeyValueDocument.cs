using LinqInfer.Data;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data
{
    internal class KeyValueDocument : IExportableAsDataDocument, IImportableFromDataDocument
    {
        private readonly IDictionary<string, string> _keyValues;

        public KeyValueDocument() : this(new Dictionary<string, string>())
        {
        }

        public KeyValueDocument(IDictionary<string, string> keyValues)
        {
            _keyValues = keyValues;
        }

        public IDictionary<string, string> Data => _keyValues;

        public PortableDataDocument ToDataDocument()
        {
            var doc = new PortableDataDocument();

            foreach (var kv in _keyValues)
            {
                doc.Properties[kv.Key] = kv.Value;
            }

            return doc;
        }

        public void FromDataDocument(PortableDataDocument doc)
        {
            _keyValues.Clear();

            foreach (var prop in doc.Properties.Where(p => !p.Key.StartsWith("_")))
            {
                _keyValues[prop.Key] = prop.Value;
            }
        }
    }
}