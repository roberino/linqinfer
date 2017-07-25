using LinqInfer.Microservices.Resources;
using LinqInfer.Text;
using System.Collections.Generic;

namespace LinqInfer.Microservices.Text
{
    public class DocumentIndexView : ResourceHeader
    {
        private readonly IDocumentIndex _index;

        public DocumentIndexView(IDocumentIndex index, string indexName)
        {
            IndexName = indexName;
            _index = index;
        }

        public bool Exists { get { return _index != null; } }

        public string IndexName { get; set; }

        public long DocumentCount { get { return _index == null ? 0 : _index.DocumentCount; } }

        public IDictionary<string, long> Terms { get { return _index == null ? new Dictionary<string, long>() : _index.GetTermFrequencies(); } }
    }
}