using LinqInfer.Text;
using System.Collections.Generic;

namespace LinqInfer.AspNetCoreTestHarness.Text
{
    public class DocumentIndexViewModel
    {
        private readonly IDocumentIndex _index;

        public DocumentIndexViewModel(IDocumentIndex index, string indexName)
        {
            IndexName = indexName;
            _index = index;
        }

        public string IndexName { get; set; }

        public long DocumentCount { get { return _index.DocumentCount; } }

        public IDictionary<string, long> Terms { get { return _index.GetTermFrequencies(); } }
    }
}