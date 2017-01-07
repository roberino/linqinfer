using LinqInfer.Data;
using System.Collections.Generic;

namespace LinqInfer.Text
{
    public interface IDocumentIndex : IBinaryPersistable, IXmlExportable, IXmlImportable
    {
        /// <summary>
        /// Indexes a single <see cref="TokenisedTextDocument"/> 
        /// </summary>
        void IndexDocument(TokenisedTextDocument document);

        /// <summary>
        /// Indexes multiple <see cref="TokenisedTextDocument"/> 
        /// </summary>
        void IndexDocuments(IEnumerable<TokenisedTextDocument> documents);

        /// <summary>
        /// Finds relevant documents based on the terms within the query
        /// and using the weighting method defined by the index
        /// </summary>
        IEnumerable<SearchResult> Search(string query);

        /// <summary>
        /// Returns a set of terms as a <see cref="ISemanticSet"/>
        /// </summary>
        ISemanticSet ExtractTerms();
    }
}