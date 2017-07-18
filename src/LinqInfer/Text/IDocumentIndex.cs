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
        /// Gets the raw weighting data for each term within a given query
        /// </summary>
        /// <param name="query">The query text</param>
        /// <returns>A dictionary of terms and <see cref="DocumentTermWeightingData"/></returns>
        IDictionary<string, IList<DocumentTermWeightingData>> GetQueryWeightingData(string query);

        /// <summary>
        /// Returns a set of terms as a <see cref="ISemanticSet"/>
        /// </summary>
        ISemanticSet ExtractTerms();

        /// <summary>
        /// Returns a set of key terms based on their tf/idf
        /// </summary>
        /// <param name="maxNumberOfTerms">The max number of terms to return</param>
        /// <returns></returns>
        ISemanticSet ExtractKeyTerms(int maxNumberOfTerms);
    }
}