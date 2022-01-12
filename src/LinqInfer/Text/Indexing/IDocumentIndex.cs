using System.Collections.Generic;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Text.Indexing
{
    public interface IDocumentIndex : IBinaryPersistable, IXmlExportable, IXmlImportable
    {
        /// <summary>
        /// Returns the document count
        /// </summary>
        long DocumentCount { get; }

        /// <summary>
        /// Gets the tokeniser used to index new untokenised documents
        /// </summary>
        ITokeniser Tokeniser { get; }

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
        /// Returns a set of terms and total frequencies for each across all documents
        /// </summary>
        IDictionary<string, long> GetTermFrequencies();

        /// <summary>
        /// Returns a set of terms as a <see cref="ISemanticSet"/>
        /// </summary>
        IImportableExportableSemanticSet ExtractTerms();

        /// <summary>
        /// Returns a set of key terms (the default implementation returns words ordered descending by their tf/idf)
        /// </summary>
        /// <param name="maxNumberOfTerms">The max number of terms to return</param>
        /// <returns></returns>
        IImportableExportableSemanticSet ExtractKeyTerms(int maxNumberOfTerms);
    }
}