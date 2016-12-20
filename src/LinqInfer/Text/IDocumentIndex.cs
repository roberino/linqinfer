using LinqInfer.Data;
using System.Collections.Generic;

namespace LinqInfer.Text
{
    public interface IDocumentIndex : IBinaryPersistable, IXmlExportable, IXmlImportable
    {
        void IndexDocument(TokenisedTextDocument document);
        void IndexDocuments(IEnumerable<TokenisedTextDocument> documents);
        IEnumerable<SearchResult> Search(string query);
    }
}